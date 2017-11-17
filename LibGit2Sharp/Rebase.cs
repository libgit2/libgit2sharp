using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    public class Rebase : IDisposable
    {
        private readonly Repository repository;
        private readonly RebaseHandle handle;
        private bool disposed = false;

        public Rebase(Repository repository, Branch branch, Branch upstream, Branch onto, RebaseOptions options)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(upstream, "upstream");

            this.repository = repository;

            options = options ?? new RebaseOptions();

            using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
            {
                GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                {
                    version = 1,
                    checkout_options = checkoutOptionsWrapper.Options,
                };

                using (ReferenceHandle branchRefPtr = RefHandleFromBranch(repository, branch))
                using (ReferenceHandle upstreamRefPtr = RefHandleFromBranch(repository, upstream))
                using (ReferenceHandle ontoRefPtr = RefHandleFromBranch(repository, onto))
                using (AnnotatedCommitHandle annotatedBranchCommitHandle = AnnotatedCommitHandleFromRefHandle(repository, branchRefPtr))
                using (AnnotatedCommitHandle upstreamRefAnnotatedCommitHandle = AnnotatedCommitHandleFromRefHandle(repository, upstreamRefPtr))
                using (AnnotatedCommitHandle ontoRefAnnotatedCommitHandle = AnnotatedCommitHandleFromRefHandle(repository, ontoRefPtr))
                {
                    handle = Proxy.git_rebase_init(repository.Handle,
                        annotatedBranchCommitHandle,
                        upstreamRefAnnotatedCommitHandle,
                        ontoRefAnnotatedCommitHandle,
                        gitRebaseOptions);
                }
            }
        }

        private static ReferenceHandle RefHandleFromBranch(Repository repository, Branch branch)
        {
            return (branch == null) ?
                null :
                repository.Refs.RetrieveReferencePtr(branch.CanonicalName);
        }

        private unsafe static AnnotatedCommitHandle AnnotatedCommitHandleFromRefHandle(Repository repository, ReferenceHandle refHandle)
        {
            return (refHandle == null) ?
                new AnnotatedCommitHandle(null, false) :
                Proxy.git_annotated_commit_from_ref(repository.Handle, refHandle);
        }

        public virtual unsafe RebaseStepInfo Next()
        {
            git_rebase_operation* rebaseOp = Proxy.git_rebase_next(handle);

            if (rebaseOp == null)
            {
                return null;
            }

            return new RebaseStepInfo(rebaseOp->type,
                repository.Lookup<Commit>(ObjectId.BuildFromPtr(&rebaseOp->id)),
                LaxUtf8NoCleanupMarshaler.FromNative(rebaseOp->exec));
        }

        public virtual ObjectId Commit(Identity author, Identity committer, string message)
        {
            Ensure.ArgumentNotNull(committer, "committer");

            Proxy.GitRebaseCommitResult rebaseResult = Proxy.git_rebase_commit(handle, author, committer, message);
            return rebaseResult.WasPatchAlreadyApplied ? null : new ObjectId(rebaseResult.CommitId);
        }

        public virtual void Abort()
        {
            Proxy.git_rebase_abort(handle);
        }

        /// <summary>
        /// Get the index of the current step in the rebase process.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
        /// <returns>The index</returns>
        public virtual long CurrentStep
        {
            get
            {
                return Proxy.git_rebase_operation_current(handle);
            }
        }

        /// <summary>
        /// Get the number of steps in the rebase process.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
        /// <returns>The number of steps in the rebase operation.</returns>
        public virtual long TotalSteps
        {
            get
            {
                return Proxy.git_rebase_operation_entrycount(handle);
            }
        }

        /// <summary>
        /// The info on the current step.
        /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
        /// </summary>
        public virtual unsafe RebaseStepInfo CurrentStepInfo
        {
            get
            {
                return GetStepInfo(CurrentStep);
            }
        }

        /// <summary>
        /// Get info on the specified step
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
        /// <param name="stepIndex">The step number to get information about.</param>
        /// <returns></returns>
        public virtual unsafe RebaseStepInfo GetStepInfo(long stepIndex)
        {
            git_rebase_operation* gitRebasestepInfo = Proxy.git_rebase_operation_byindex(handle, stepIndex);
            return new RebaseStepInfo(gitRebasestepInfo->type,
                repository.Lookup<Commit>(ObjectId.BuildFromPtr(&gitRebasestepInfo->id)),
                LaxUtf8Marshaler.FromNative(gitRebasestepInfo->exec));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                handle.SafeDispose();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Rebase()
        {
            Dispose(false);
        }
    }
}
