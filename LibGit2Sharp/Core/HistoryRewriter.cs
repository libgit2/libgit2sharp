using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp.Core
{
    internal class HistoryRewriter
    {
        private readonly Repository repo;

        private readonly HashSet<Commit> targetedCommits;
        private readonly Dictionary<ObjectId, ObjectId> shaMap = new Dictionary<ObjectId, ObjectId>();

        private readonly Func<Commit, CommitRewriteInfo> headerRewriter;
        private readonly Func<Commit, TreeDefinition> treeRewriter;
        private readonly string backupRefsNamespace;
        private readonly Func<IEnumerable<Commit>, IEnumerable<Commit>> parentsRewriter;
        private readonly Func<String, bool, GitObject, string> tagNameRewriter;

        public HistoryRewriter(
            Repository repo,
            IEnumerable<Commit> commitsToRewrite,
            Func<Commit, CommitRewriteInfo> headerRewriter,
            Func<Commit, TreeDefinition> treeRewriter,
            Func<IEnumerable<Commit>, IEnumerable<Commit>> parentsRewriter,
            Func<String, bool, GitObject, string> tagNameRewriter,
            string backupRefsNamespace)
        {
            this.repo = repo;
            targetedCommits = new HashSet<Commit>(commitsToRewrite);

            this.headerRewriter = headerRewriter ?? CommitRewriteInfo.From;
            this.treeRewriter = treeRewriter;
            this.tagNameRewriter = tagNameRewriter;
            this.parentsRewriter = parentsRewriter ?? (ps => ps);

            this.backupRefsNamespace = backupRefsNamespace;
        }

        public void Execute()
        {
            // Find out which refs lead to at least one the commits
            var refsToRewrite = repo.Refs.ReachableFrom(targetedCommits).ToList();

            var filter = new Filter
                             {
                                 Since = refsToRewrite,
                                 SortBy = GitSortOptions.Reverse | GitSortOptions.Topological
                             };

            var commits = repo.Commits.QueryBy(filter);
            foreach (var commit in commits)
            {
                RewriteCommit(commit);
            }

            // Rewrite the refs
            var refsToRollBack = new Dictionary<Reference, Tuple<Reference, string>>();

            try
            {
                // Ordering matters. In the case of `A -> B -> commit`, we need to make sure B is rewritten
                // before A.
                foreach (var reference in refsToRewrite.OrderBy(ReferenceDepth))
                {
                    //TODO: Check how rewriting of notes actually behaves

                    var dref = reference as DirectReference;
                    if (dref == null)
                    {
                        //TODO: Handle a cornercase where a symbolic reference
                        //      points to a Tag which name has been rewritten
                        continue;
                    }

                    var newTarget = RewriteTarget(dref.Target);

                    Tuple<Reference, string> updateInfo = RewriteReference(dref, newTarget, backupRefsNamespace);

                    refsToRollBack.Add(dref, updateInfo);
                }
            }
            catch (Exception)
            {
                // Something went wrong. Roll back the rewrites
                foreach (var kvp in refsToRollBack)
                {
                    var oldRef = kvp.Key;
                    var rewrittenRef = kvp.Value.Item1;

                    var dRef = oldRef as DirectReference;
                    if (dRef != null)
                    {
                        repo.Refs.UpdateTarget(rewrittenRef, dRef.Target.Id, "filter-branch: abort");

                        if (rewrittenRef.CanonicalName != oldRef.CanonicalName)
                        {
                            repo.Refs.Move(rewrittenRef, oldRef.CanonicalName);
                        }
                    }
                    else
                    {
                        // TODO: This isn't covered by any test
                        throw new NotSupportedException();
                    }

                    repo.Refs.Remove(kvp.Value.Item2);
                }

                throw;
            }
        }

        private Tuple<Reference, string> RewriteReference(DirectReference oldRef, ObjectId newTarget, string namePrefix)
        {
            string backupName = namePrefix + oldRef.CanonicalName.Substring("refs/".Length);

            if (repo.Refs.Resolve<Reference>(backupName) != null)
            {
                throw new InvalidOperationException(
                    String.Format("Can't back up reference '{0}' - '{1}' already exists", oldRef.CanonicalName, backupName));
            }

            string newTagName = oldRef.CanonicalName;
            if (oldRef.IsTag() && tagNameRewriter != null)
            {
                newTagName = Reference.TagPrefix +
                             tagNameRewriter(oldRef.CanonicalName.Substring(Reference.TagPrefix.Length), false, oldRef.Target);
            }

            repo.Refs.Add(backupName, oldRef.TargetIdentifier, false, "filter-branch: backup");

            Reference newRef = repo.Refs.UpdateTarget(oldRef, newTarget, "filter-branch: rewrite");

            if (!oldRef.IsTag() || newRef.CanonicalName == newTagName)
            {
                return new Tuple<Reference, string>(newRef, backupName);
            }

            Reference newTag = repo.Refs.Move(newRef, newTagName);

            return new Tuple<Reference, string>(newTag, backupName);
        }

        private void RewriteCommit(Commit commit)
        {
            var newHeader = CommitRewriteInfo.From(commit);
            var newTree = commit.Tree;

            // Find the new parents
            var newParents = commit.Parents
                .Select(oldParent =>
                        shaMap.ContainsKey(oldParent.Id)
                            ? shaMap[oldParent.Id]
                            : oldParent.Id)
                .Select(id => repo.Lookup<Commit>(id));

            if (targetedCommits.Contains(commit))
            {
                // Get the new commit header
                newHeader = headerRewriter(commit);

                if (treeRewriter != null)
                {
                    // Get the new commit tree
                    var newTreeDefinition = treeRewriter(commit);
                    newTree = repo.ObjectDatabase.CreateTree(newTreeDefinition);
                }

                // Retrieve new parents
                newParents = parentsRewriter(newParents);
            }

            // Create the new commit
            var newCommit = repo.ObjectDatabase.CreateCommit(newHeader.Message, newHeader.Author,
                                                             newHeader.Committer, newTree,
                                                             newParents);

            // Record the rewrite
            shaMap[commit.Id] = newCommit.Id;
        }

        private ObjectId RewriteTarget(GitObject oldTarget)
        {
            // Has this target already been rewritten?
            if (shaMap.ContainsKey(oldTarget.Id))
            {
                return shaMap[oldTarget.Id];
            }

            Debug.Assert((oldTarget as Commit) == null);

            var annotation = oldTarget as TagAnnotation;
            if (annotation == null)
            {
                //TODO: Probably a Tree or a Blob. This is not covered by any test
                return oldTarget.Id;
            }

            // Recursively rewrite annotations if necessary
            ObjectId newTargetId = RewriteTarget(annotation.Target);

            var newTarget = repo.Lookup(newTargetId);

            string newName = annotation.Name;

            if (tagNameRewriter != null)
            {
                newName = tagNameRewriter(annotation.Name, true, annotation.Target);
            }

            var newAnnotation = repo.ObjectDatabase.CreateTag(newName, newTarget, annotation.Tagger,
                                                              annotation.Message);
            shaMap[annotation.Id] = newAnnotation.Id;
            return newAnnotation.Id;
        }

        private int ReferenceDepth(Reference reference)
        {
            var dref = reference as DirectReference;
            return dref == null
                       ? 1 + ReferenceDepth(((SymbolicReference)reference).Target)
                       : 1;
        }
    }
}
