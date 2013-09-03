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
        private readonly Dictionary<Reference, Reference> refMap = new Dictionary<Reference, Reference>();
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

        private Reference RewriteReference(Reference reference)
        {
            // Has this target already been rewritten?
            if (refMap.ContainsKey(reference))
            {
                return refMap[reference];
            }

            var sref = reference as SymbolicReference;
            if (sref != null)
            {
                return RewriteReference(
                    sref, old => old.Target, RewriteReference,
                    (refs, old, target, logMessage) => refs.UpdateTarget(old, target, logMessage));
            }

            var dref = reference as DirectReference;
            if (dref != null)
            {
                return RewriteReference(
                    dref, old => old.Target, RewriteTarget,
                    (refs, old, target, logMessage) => refs.UpdateTarget(old, target.Id, logMessage));
            }

            return reference;
        }

        private Reference RewriteReference<TRef, TTarget>(
            TRef oldRef, Func<TRef, TTarget> selectTarget,
            Func<TTarget, TTarget> rewriteTarget,
            Func<ReferenceCollection, TRef, TTarget, string, Reference> updateTarget)
            where TRef : Reference
            where TTarget : class
        {
            var oldRefTarget = selectTarget(oldRef);

            string newRefName = oldRef.CanonicalName;
            if (oldRef.IsTag() && options.TagNameRewriter != null)
            {
                newRefName = Reference.TagPrefix +
                             options.TagNameRewriter(oldRef.CanonicalName.Substring(Reference.TagPrefix.Length),
                                                     false, oldRef.TargetIdentifier);
            }

            var newTarget = rewriteTarget(oldRefTarget);

            if (oldRefTarget.Equals(newTarget) && oldRef.CanonicalName == newRefName)
            {
                // The reference isn't rewritten
                return oldRef;
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
                return refMap[oldRef] = null;
            }

            Reference newRef = updateTarget(repo.Refs, oldRef, newTarget, "filter-branch: rewrite");
            rollbackActions.Enqueue(() => updateTarget(repo.Refs, oldRef, oldRefTarget, "filter-branch: abort"));

            if (newRef.CanonicalName == newRefName)
            {
                return refMap[oldRef] = newRef;
            }

            var movedRef = repo.Refs.Move(newRef, newRefName);
            rollbackActions.Enqueue(() => repo.Refs.Move(newRef, oldRef.CanonicalName));
            return refMap[oldRef] = movedRef;
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
