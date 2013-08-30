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
        private readonly Dictionary<GitObject, GitObject> objectMap = new Dictionary<GitObject, GitObject>();
        private readonly Queue<Action> rollbackActions = new Queue<Action>();

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

            try
            {
                // Ordering matters. In the case of `A -> B -> commit`, we need to make sure B is rewritten
                // before A.
                foreach (var reference in refsToRewrite.OrderBy(ReferenceDepth))
                {
                    // TODO: Check how rewriting of notes actually behaves

                    RewriteReference(reference);
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
            finally
            {
                rollbackActions.Clear();
            }
        }

        private void RewriteReference(Reference reference)
        {
            var sref = reference as SymbolicReference;
            if (sref != null)
            {
                // TODO: Handle a cornercase where a symbolic reference
                //       points to a Tag which name has been rewritten
                return;
            }

            var dref = reference as DirectReference;
            if (dref != null)
            {
                RewriteReference(dref);
            }
        }

        private void RewriteReference(DirectReference oldRef)
        {
            string newRefName = oldRef.CanonicalName;
            if (oldRef.IsTag() && options.TagNameRewriter != null)
            {
                newRefName = Reference.TagPrefix +
                             options.TagNameRewriter(oldRef.CanonicalName.Substring(Reference.TagPrefix.Length),
                                                     false, oldRef.TargetIdentifier);
            }

            var newTarget = RewriteTarget(oldRef.Target);

            if (oldRef.Target == newTarget && oldRef.CanonicalName == newRefName)
            {
                // The reference isn't rewritten
                return;
            }

            string backupName = backupRefsNamespace + oldRef.CanonicalName.Substring("refs/".Length);

            if (repo.Refs.Resolve<Reference>(backupName) != null)
            {
                throw new InvalidOperationException(
                    String.Format("Can't back up reference '{0}' - '{1}' already exists", oldRef.CanonicalName, backupName));
            }

            repo.Refs.Add(backupName, oldRef.TargetIdentifier, false, "filter-branch: backup");
            rollbackActions.Enqueue(() => repo.Refs.Remove(backupName));

            if (newTarget == null)
            {
                repo.Refs.Remove(oldRef);
                rollbackActions.Enqueue(() => repo.Refs.Add(oldRef.CanonicalName, oldRef, true, "filter-branch: abort"));
                return;
            }

            Reference newRef = repo.Refs.UpdateTarget(oldRef, newTarget.Id, "filter-branch: rewrite");
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
            var newParents = commit.Parents;

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
            var mappedNewParents = newParents
                .Select(oldParent =>
                        objectMap.ContainsKey(oldParent)
                            ? objectMap[oldParent] as Commit
                            : oldParent)
                .Where(newParent => newParent != null)
                .ToList();

            if (options.PruneEmptyCommits &&
                TryPruneEmptyCommit(commit, mappedNewParents, newTree))
            {
                return;
            }

            var newCommit = repo.ObjectDatabase.CreateCommit(newHeader.Message, newHeader.Author,
                                                             newHeader.Committer, newTree,
                                                             mappedNewParents);

            // Record the rewrite
            objectMap[commit] = newCommit;
        }

        private bool TryPruneEmptyCommit(Commit commit, IList<Commit> mappedNewParents, Tree newTree)
        {
            var parent = mappedNewParents.Count > 0 ? mappedNewParents[0] : null;

            if (parent == null)
            {
                if (newTree.Count == 0)
                {
                    objectMap[commit] = null;
                    return true;
                }
            }
            else if (parent.Tree == newTree)
            {
                objectMap[commit] = parent;
                return true;
            }

            return false;
        }

        private GitObject RewriteTarget(GitObject oldTarget)
        {
            // Has this target already been rewritten?
            if (objectMap.ContainsKey(oldTarget))
            {
                return objectMap[oldTarget];
            }

            Debug.Assert((oldTarget as Commit) == null);

            var annotation = oldTarget as TagAnnotation;
            if (annotation == null)
            {
                //TODO: Probably a Tree or a Blob. This is not covered by any test
                return oldTarget;
            }

            // Recursively rewrite annotations if necessary
            var newTarget = RewriteTarget(annotation.Target);

            string newName = annotation.Name;

            if (options.TagNameRewriter != null)
            {
                newName = options.TagNameRewriter(annotation.Name, true, annotation.Target.Sha);
            }

            var newAnnotation = repo.ObjectDatabase.CreateTagAnnotation(newName, newTarget, annotation.Tagger,
                                                              annotation.Message);
            objectMap[annotation] = newAnnotation;
            return newAnnotation;
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
