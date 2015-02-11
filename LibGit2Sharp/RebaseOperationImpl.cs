using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    internal class RebaseOperationImpl
    {
        private RebaseSafeHandle rebaseOperationHandle;
        private Repository repository;
        private Signature committer;
        private RebaseOptions options;

        public RebaseOperationImpl(RebaseSafeHandle rebaseOp, Repository repo, Signature committer, RebaseOptions options)
        {
            Ensure.ArgumentNotNull(rebaseOp, "rebaseOp");
            Ensure.ArgumentNotNull(repo, "repo");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNull(options, "options");

            this.rebaseOperationHandle = rebaseOp;
            this.repository = repo;
            this.committer = committer;
            this.options = options;
        }

        /// <summary>
        /// Run a rebase to completion or conflict.
        /// </summary>
        /// <returns>true if completed successfully, false if conflicts encountered.</returns>
        public RebaseResult Run()
        {
            GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options);
            GitCheckoutOpts gitCheckoutOpts = checkoutOptionsWrapper.Options;
            RebaseResult rebaseResult = null;

            try
            {
                GitRebaseOperation rebaseOperationReport = null;

                while (rebaseResult == null)
                {
                    rebaseOperationReport = Proxy.git_rebase_next(rebaseOperationHandle, ref gitCheckoutOpts);

                    int currentStepIndex = Proxy.git_rebase_operation_current(rebaseOperationHandle);
                    int totalStepCount = Proxy.git_rebase_operation_entrycount(rebaseOperationHandle);

                    if (rebaseOperationReport == null)
                    {
                        GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                        {
                            version = 1,
                        };

                        // Rebase is completed!
                        // currentStep is the last completed - increment it to account
                        // for the fact that we have moved past last step index.
                        Proxy.git_rebase_finish(rebaseOperationHandle, null, gitRebaseOptions);
                        rebaseResult = new RebaseResult(RebaseStatus.Complete,
                                                        currentStepIndex + 1,
                                                        totalStepCount,
                                                        null);
                    }
                    else
                    {
                        RebaseStepInfo stepInfo = new RebaseStepInfo(rebaseOperationReport.type,
                                                                     new ObjectId(rebaseOperationReport.id),
                                                                     LaxUtf8NoCleanupMarshaler.FromNative(rebaseOperationReport.exec),
                                                                     currentStepIndex,
                                                                     totalStepCount);

                        if (options.RebaseStepStarting != null)
                        {
                            options.RebaseStepStarting(new BeforeRebaseStepInfo(stepInfo));
                        }

                        switch (rebaseOperationReport.type)
                        {
                            case RebaseStepOperation.Pick:
                                // commit and continue.
                                if (repository.Index.IsFullyMerged)
                                {
                                    GitOid id = Proxy.git_rebase_commit(rebaseOperationHandle, null, committer);

                                    // Report that we just completed the step
                                    if (options.RebaseStepCompleted != null)
                                    {
                                        options.RebaseStepCompleted(new AfterRebaseStepInfo(stepInfo, new ObjectId(id)));
                                    }
                                }
                                else
                                {
                                    rebaseResult = new RebaseResult(RebaseStatus.Conflicts,
                                                                    currentStepIndex,
                                                                    totalStepCount,
                                                                    null);
                                }
                                break;
                            case RebaseStepOperation.Squash:
                            case RebaseStepOperation.Edit:
                            case RebaseStepOperation.Exec:
                            case RebaseStepOperation.Fixup:
                            case RebaseStepOperation.Reword:
                                // These operations are not yet supported by the library.
                                throw new LibGit2SharpException(string.Format(
                                    "Rebase Operation Type ({0}) is not currently supported in LibGit2Sharp.",
                                    rebaseOperationReport.type));
                            default:
                                throw new ArgumentException(string.Format(
                                    "Unexpected Rebase Operation Type: {0}", rebaseOperationReport.type));
                        }
                    }
                }
            }
            finally
            {
                checkoutOptionsWrapper.SafeDispose();
                checkoutOptionsWrapper = null;
            }

            return rebaseResult;
        }
    }
}
