using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LibGit2Sharp.Core
{
    internal class HistoryRewriter
    {
        private readonly Repository repo;

        private readonly HashSet<Commit> targetedCommits;
        private readonly Dictionary<ObjectId, ObjectId> shaMap = new Dictionary<ObjectId, ObjectId>();

        private readonly string backupRefsNamespace;
        private readonly RewriteHistoryOptions options;

        public HistoryRewriter(
            Repository repo,
            IEnumerable<Commit> commitsToRewrite,
            RewriteHistoryOptions options)
        {
            this.repo = repo;
            this.options = options;
            targetedCommits = new HashSet<Commit>(commitsToRewrite);

            backupRefsNamespace = this.options.BackupRefsNamespace;

            if (!backupRefsNamespace.EndsWith("/", StringComparison.Ordinal))
            {
                backupRefsNamespace += "/";
            }
        }

        public void Execute()
        {
            // Find out which refs lead to at least one the commits
            var refsToRewrite = repo.Refs.ReachableFrom(targetedCommits).ToList();

            var filter = new CommitFilter
                             {
                                 Since = refsToRewrite,
                                 SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological
                             };

            var commits = repo.Commits.QueryBy(filter);
            foreach (var commit in commits)
            {
                RewriteCommit(commit);
            }

            var rollbackActions = new Queue<Action>();

            try
            {
                // Ordering matters. In the case of `A -> B -> commit`, we need to make sure B is rewritten
                // before A.
                foreach (var reference in refsToRewrite.OrderBy(ReferenceDepth))
                {
                    // TODO: Check how rewriting of notes actually behaves

                    var dref = reference as DirectReference;
                    if (dref == null)
                    {
                        // TODO: Handle a cornercase where a symbolic reference
                        //       points to a Tag which name has been rewritten
                        continue;
                    }

                    var newTarget = RewriteTarget(dref.Target);

                    RewriteReference(dref, newTarget, backupRefsNamespace, rollbackActions);
                }
            }
            catch (Exception)
            {
                foreach (var action in rollbackActions)
                {
                    action();
                }

                throw;
            }
        }

        private void RewriteReference(DirectReference oldRef, ObjectId newTarget, string backupNamePrefix, Queue<Action> rollbackActions)
        {
            string newRefName = oldRef.CanonicalName;
            if (oldRef.IsTag() && options.TagNameRewriter != null)
            {
                newRefName = Reference.TagPrefix +
                             options.TagNameRewriter(oldRef.CanonicalName.Substring(Reference.TagPrefix.Length), false, oldRef.Target);
            }

            if (oldRef.Target.Id == newTarget && oldRef.CanonicalName == newRefName)
            {
                // The reference isn't rewritten
                return;
            }

            string backupName = backupNamePrefix + oldRef.CanonicalName.Substring("refs/".Length);

            if (repo.Refs.Resolve<Reference>(backupName) != null)
            {
                throw new InvalidOperationException(
                    String.Format("Can't back up reference '{0}' - '{1}' already exists", oldRef.CanonicalName, backupName));
            }

            repo.Refs.Add(backupName, oldRef.TargetIdentifier, false, "filter-branch: backup");
            rollbackActions.Enqueue(() => repo.Refs.Remove(backupName));

            Reference newRef = repo.Refs.UpdateTarget(oldRef, newTarget, "filter-branch: rewrite");
            rollbackActions.Enqueue(() => repo.Refs.UpdateTarget(oldRef, oldRef.Target.Id, "filter-branch: abort"));

            if (newRef.CanonicalName == newRefName)
            {
                return;
            }

            repo.Refs.Move(newRef, newRefName);
            rollbackActions.Enqueue(() => repo.Refs.Move(newRef, oldRef.CanonicalName));
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
                if (options.CommitHeaderRewriter != null)
                {
                    newHeader = options.CommitHeaderRewriter(commit) ?? newHeader;
                }

                if (options.CommitTreeRewriter != null)
                {
                    // Get the new commit tree
                    var newTreeDefinition = options.CommitTreeRewriter(commit);
                    newTree = repo.ObjectDatabase.CreateTree(newTreeDefinition);
                }

                // Retrieve new parents
                if (options.CommitParentsRewriter != null)
                {
                    newParents = options.CommitParentsRewriter(commit);
                }
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

            if (options.TagNameRewriter != null)
            {
                newName = options.TagNameRewriter(annotation.Name, true, annotation.Target);
            }

            var newAnnotation = repo.ObjectDatabase.CreateTagAnnotation(newName, newTarget, annotation.Tagger,
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
