using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The type of operation to be performed in a rebase step.
    /// </summary>
    public enum RebaseStepOperation
    {
        /// <summary>
        /// Commit is to be cherry-picked.
        /// </summary>
        Pick = 0,

        /// <summary>
        /// Cherry-pick the commit and edit the commit message.
        /// </summary>
        Reword,

        /// <summary>
        /// Cherry-pick the commit but allow user to edit changes.
        /// </summary>
        Edit,

        /// <summary>
        /// Commit is to be squashed into previous commit. The commit
        /// message will be merged with the previous message.
        /// </summary>
        Squash,

        /// <summary>
        /// Commit is to be squashed into previous commit. The commit
        /// message will be discarded.
        /// </summary>
        Fixup,

        /// <summary>
        /// No commit to cherry-pick. Run the given command and continue
        /// if successful.
        /// </summary>
        Exec
    }

    /// <summary>
    /// Encapsulates a rebase operation.
    /// </summary>
    public class RebaseOperation
    {
        internal readonly Repository repository;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RebaseOperation()
        { }

        internal RebaseOperation(Repository repo)
        {
            this.repository = repo;
        }

        /// <summary>
        /// Continue the current rebase.
        /// </summary>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="options">The <see cref="RebaseOptions"/> that specify the commit behavior.</param>
        public virtual RebaseResult Continue(Signature committer, RebaseOptions options)
        {
            Ensure.ArgumentNotNull(committer, "committer");

            options = options ?? new RebaseOptions();

            RebaseSafeHandle rebase = null;
            try
            {
                rebase = Proxy.git_rebase_open(repository.Handle);

                // Get information on the current step
                int currentStepIndex = Proxy.git_rebase_operation_current(rebase);
                int totalStepCount = Proxy.git_rebase_operation_entrycount(rebase);
                GitRebaseOperation gitRebasestepInfo = Proxy.git_rebase_operation_byindex(rebase, currentStepIndex);

                GitOid id = Proxy.git_rebase_commit(rebase, null, committer);

                // Report that we just completed the step
                if (options.RebaseStepCompleted != null)
                {
                    var stepInfo = new RebaseStepInfo(gitRebasestepInfo.type,
                                                      new ObjectId(gitRebasestepInfo.id),
                                                      LaxUtf8NoCleanupMarshaler.FromNative(gitRebasestepInfo.exec),
                                                      currentStepIndex,
                                                      totalStepCount);
                    options.RebaseStepCompleted(new AfterRebaseStepInfo(stepInfo, new ObjectId(id)));
                }

                RebaseResult rebaseResult = RebaseOperationImpl.Run(rebase, repository, committer, options);
                return rebaseResult;
            }
            finally
            {
                rebase.SafeDispose();
                rebase = null;
            }
        }

        /// <summary>
        /// Abort the rebase operation.
        /// </summary>
        /// <param name="signature">The <see cref="Signature"/> of who is reborting the rebase.</param>
        public virtual void Abort(Signature signature)
        {
            Ensure.ArgumentNotNull(signature, "signature");

            RebaseSafeHandle rebase = null;
            try
            {
                rebase = Proxy.git_rebase_open(repository.Handle);
                Proxy.git_rebase_abort(rebase, signature);
            }
            finally
            {
                rebase.SafeDispose();
                rebase = null;
            }
        }

        /// <summary>
        /// Skip this rebase step.
        /// </summary>
        public virtual void Skip()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The info on the current step.
        /// </summary>
        public virtual RebaseStepInfo CurrentStepInfo
        {
            get
            {
                RebaseSafeHandle rebaseHandle = null;

                try
                {
                    rebaseHandle = Proxy.git_rebase_open(repository.Handle);
                    int currentStepIndex = Proxy.git_rebase_operation_current(rebaseHandle);
                    int totalStepCount = Proxy.git_rebase_operation_entrycount(rebaseHandle);
                    GitRebaseOperation gitRebasestepInfo = Proxy.git_rebase_operation_byindex(rebaseHandle, currentStepIndex);
                    var stepInfo = new RebaseStepInfo(gitRebasestepInfo.type,
                                                      gitRebasestepInfo.id,
                                                      LaxUtf8NoCleanupMarshaler.FromNative(gitRebasestepInfo.exec),
                                                      currentStepIndex,
                                                      totalStepCount);
                    return stepInfo;
                }
                finally
                {
                    rebaseHandle.SafeDispose();
                    rebaseHandle = null;
                }
            }
        }
    }
}
