using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace LibGit2Sharp.Core
{
    internal class HistoryRewriter
    {
        private readonly IRepository repo;

        private readonly HashSet<Commit> targetedCommits;
        private readonly Dictionary<GitObject, GitObject> objectMap = new Dictionary<GitObject, GitObject>();
        private readonly Dictionary<Reference, Reference> refMap = new Dictionary<Reference, Reference>();
        private readonly Queue<Action> rollbackActions = new Queue<Action>();

        private readonly string backupRefsNamespace;
        private readonly RewriteHistoryOptions options;

        public HistoryRewriter(
            IRepository repo,
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
            var success = false;
            try
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

                // Ordering matters. In the case of `A -> B -> commit`, we need to make sure B is rewritten
                // before A.
                foreach (var reference in refsToRewrite.OrderBy(ReferenceDepth))
                {
                    // TODO: Check how rewriting of notes actually behaves

                    RewriteReference(reference);
                }

                success = true;
                if (options.OnSucceeding != null)
                {
                    options.OnSucceeding();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (!success && options.OnError != null)
                    {
                        options.OnError(ex);
                    }
                }
                finally
                {
                    foreach (var action in rollbackActions)
                    {
                        action();
                    }
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
                    (refs, old, target, sig, logMessage) => refs.UpdateTarget(old, target, sig, logMessage));
            }

            var dref = reference as DirectReference;
            if (dref != null)
            {
                return RewriteReference(
                    dref, old => old.Target, RewriteTarget,
                    (refs, old, target, sig, logMessage) => refs.UpdateTarget(old, target.Id, sig, logMessage));
            }

            return reference;
        }

        private delegate Reference ReferenceUpdater<in TRef, in TTarget>(
            ReferenceCollection refs, TRef origRef, TTarget origTarget, Signature signature, string logMessage)
            where TRef : Reference
            where TTarget : class;

        private Reference RewriteReference<TRef, TTarget>(
            TRef oldRef, Func<TRef, TTarget> selectTarget,
            Func<TTarget, TTarget> rewriteTarget,
            ReferenceUpdater<TRef, TTarget> updateTarget)
            where TRef : Reference
            where TTarget : class
        {
            var oldRefTarget = selectTarget(oldRef);
            var signature = repo.Config.BuildSignature(DateTimeOffset.Now);

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
                    String.Format(
                        CultureInfo.InvariantCulture, "Can't back up reference '{0}' - '{1}' already exists",
                        oldRef.CanonicalName, backupName));
            }

            repo.Refs.Add(backupName, oldRef.TargetIdentifier, signature, "filter-branch: backup");
            rollbackActions.Enqueue(() => repo.Refs.Remove(backupName));

            if (newTarget == null)
            {
                repo.Refs.Remove(oldRef);
                rollbackActions.Enqueue(() => repo.Refs.Add(oldRef.CanonicalName, oldRef, signature, "filter-branch: abort", true));
                return refMap[oldRef] = null;
            }

            Reference newRef = updateTarget(repo.Refs, oldRef, newTarget, signature, "filter-branch: rewrite");
            rollbackActions.Enqueue(() => updateTarget(repo.Refs, oldRef, oldRefTarget, signature, "filter-branch: abort"));

            if (newRef.CanonicalName == newRefName)
            {
                return refMap[oldRef] = newRef;
            }

            var movedRef = repo.Refs.Rename(newRef, newRefName);
            rollbackActions.Enqueue(() => repo.Refs.Rename(newRef, oldRef.CanonicalName));
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

            var newCommit = repo.ObjectDatabase.CreateCommit(newHeader.Author,
                                                             newHeader.Committer,
                                                             newHeader.Message,
                                                             newTree,
                                                             mappedNewParents,
                                                             true);

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
