using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal class RebaseOperationImpl
    {
        /// <summary>
        /// Run a rebase to completion, a conflict, or a requested stop point.
        /// </summary>
        /// <param name="rebaseOperationHandle">Handle to the rebase operation.</param>
        /// <param name="repository">Repository in which rebase operation is being run.</param>
        /// <param name="committer">Committer Identity to use for the rebased commits.</param>
        /// <param name="options">Options controlling rebase behavior.</param>
        /// <returns>RebaseResult that describes the result of the rebase operation.</returns>
        public static RebaseResult Run(RebaseSafeHandle rebaseOperationHandle,
            Repository repository,
            Identity committer,
            RebaseOptions options)
        {
            Ensure.ArgumentNotNull(rebaseOperationHandle, "rebaseOperationHandle");
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNull(options, "options");

            RebaseResult rebaseResult = null;

            // This loop will run until a rebase result has been set.
            while (rebaseResult == null)
            {
                RebaseProgress rebaseStepContext = NextRebaseStep(repository, rebaseOperationHandle);

                if (rebaseStepContext.current != -1)
                {
                    rebaseResult = RunRebaseStep(rebaseOperationHandle,
                                                   repository,
                                                   committer,
                                                   options,
                                                   rebaseStepContext.current,
                                                   rebaseStepContext.total);
                }
                else
                {
                    // No step to apply - need to complete the rebase.
                    rebaseResult = CompleteRebase(rebaseOperationHandle, committer, rebaseResult);
                }
            }

            return rebaseResult;
        }

        private static RebaseResult CompleteRebase(RebaseSafeHandle rebaseOperationHandle, Identity committer, RebaseResult rebaseResult)
        {
            long totalStepCount = Proxy.git_rebase_operation_entrycount(rebaseOperationHandle);
            GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
            {
                version = 1,
            };

            // Rebase is completed!
            Proxy.git_rebase_finish(rebaseOperationHandle, committer);
            rebaseResult = new RebaseResult(RebaseStatus.Complete,
                                            totalStepCount,
                                            totalStepCount,
                                            null);
            return rebaseResult;
        }

        /// <summary>
        /// Run the current rebase step. This will handle reporting that we are about to run a rebase step,
        /// identifying and running the operation for the current step, and reporting the current step is completed.
        /// </summary>
        /// <param name="rebaseOperationHandle"></param>
        /// <param name="repository"></param>
        /// <param name="committer"></param>
        /// <param name="options"></param>
        /// <param name="stepToApplyIndex"></param>
        /// <param name="totalStepCount"/>
        /// <returns></returns>
        private static RebaseResult RunRebaseStep(RebaseSafeHandle rebaseOperationHandle,
                                                  Repository repository,
                                                  Identity committer,
                                                  RebaseOptions options,
                                                  long stepToApplyIndex,
                                                  long totalStepCount)
        {
            RebaseStepResult rebaseStepResult = null;
            RebaseResult rebaseSequenceResult = null;

            GitRebaseOperation rebaseOp = Proxy.git_rebase_operation_byindex(rebaseOperationHandle, stepToApplyIndex);
            ObjectId idOfCommitBeingRebased = new ObjectId(rebaseOp.id);

            RebaseStepInfo stepToApplyInfo = new RebaseStepInfo(rebaseOp.type,
                                                                repository.Lookup<Commit>(idOfCommitBeingRebased),
                                                                LaxUtf8NoCleanupMarshaler.FromNative(rebaseOp.exec));

            // Report the rebase step we are about to perform.
            if (options.RebaseStepStarting != null)
            {
                options.RebaseStepStarting(new BeforeRebaseStepInfo(stepToApplyInfo, stepToApplyIndex, totalStepCount));
            }

            // Perform the rebase step
            GitRebaseOperation rebaseOpReport = Proxy.git_rebase_next(rebaseOperationHandle);

            // Verify that the information from the native library is consistent.
            VerifyRebaseOp(rebaseOpReport, stepToApplyInfo);

            // Handle the result
            switch (stepToApplyInfo.Type)
            {
                case RebaseStepOperation.Pick:
                    rebaseStepResult = ApplyPickStep(rebaseOperationHandle, repository, committer, options, stepToApplyInfo);
                    break;
                case RebaseStepOperation.Squash:
                case RebaseStepOperation.Edit:
                // case RebaseStepOperation.Exec:
                case RebaseStepOperation.Fixup:
                case RebaseStepOperation.Reword:
                    // These operations are not yet supported by lg2.
                    throw new LibGit2SharpException(string.Format(
                        "Rebase Operation Type ({0}) is not currently supported in LibGit2Sharp.",
                        stepToApplyInfo.Type));
                default:
                    throw new ArgumentException(string.Format(
                        "Unexpected Rebase Operation Type: {0}", stepToApplyInfo.Type));
            }

            // Report that we just completed the step
            if (options.RebaseStepCompleted != null &&
                (rebaseStepResult.Status == RebaseStepStatus.Committed ||
                rebaseStepResult.Status == RebaseStepStatus.ChangesAlreadyApplied))
            {
                if (rebaseStepResult.ChangesAlreadyApplied)
                {
                    options.RebaseStepCompleted(new AfterRebaseStepInfo(stepToApplyInfo, stepToApplyIndex, totalStepCount));
                }
                else
                {
                    options.RebaseStepCompleted(new AfterRebaseStepInfo(stepToApplyInfo,
                                                                        repository.Lookup<Commit>(new ObjectId(rebaseStepResult.CommitId)),
                                                                        stepToApplyIndex,
                                                                        totalStepCount));
                }
            }

            // If the result of the rebase step is something that requires us to stop
            // running the rebase sequence operations, then report the result.
            if (rebaseStepResult.Status == RebaseStepStatus.Conflicts)
            {
                rebaseSequenceResult = new RebaseResult(RebaseStatus.Conflicts,
                                                        stepToApplyIndex,
                                                        totalStepCount,
                                                        null);
            }

            return rebaseSequenceResult;
        }

        private static RebaseStepResult ApplyPickStep(RebaseSafeHandle rebaseOperationHandle, Repository repository, Identity committer, RebaseOptions options, RebaseStepInfo stepToApplyInfo)
        {
            RebaseStepResult rebaseStepResult;

            // commit and continue.
            if (repository.Index.IsFullyMerged)
            {
                Proxy.GitRebaseCommitResult rebase_commit_result = Proxy.git_rebase_commit(rebaseOperationHandle, null, committer);

                if (rebase_commit_result.WasPatchAlreadyApplied)
                {
                    rebaseStepResult = new RebaseStepResult(RebaseStepStatus.ChangesAlreadyApplied);
                }
                else
                {
                    rebaseStepResult = new RebaseStepResult(RebaseStepStatus.Committed, rebase_commit_result.CommitId);
                }
            }
            else
            {
                rebaseStepResult = new RebaseStepResult(RebaseStepStatus.Conflicts);
            }

            return rebaseStepResult;
        }

        /// <summary>
        /// Verify that the information in a GitRebaseOperation and a RebaseStepInfo agree
        /// </summary>
        /// <param name="rebaseOpReport"></param>
        /// <param name="stepInfo"></param>
        private static void VerifyRebaseOp(GitRebaseOperation rebaseOpReport, RebaseStepInfo stepInfo)
        {
            // The step reported via querying by index and the step returned from git_rebase_next
            // should be the same
            if (rebaseOpReport == null ||
                new ObjectId(rebaseOpReport.id) != stepInfo.Commit.Id ||
                rebaseOpReport.type != stepInfo.Type)
            {
                // This is indicative of a program error - should never happen.
                throw new LibGit2SharpException("Unexpected step info reported by running rebase step.");
            }
        }

        private struct RebaseProgress
        {
            public long current;
            public long total;
        }

        /// <summary>
        /// Returns the next rebase step, or null if there are none,
        /// and the rebase operation needs to be finished.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="rebaseOperationHandle"></param>
        /// <returns></returns>
        private static RebaseProgress NextRebaseStep(
            Repository repository,
            RebaseSafeHandle rebaseOperationHandle)
        {
            // stepBeingApplied indicates the step that will be applied by by git_rebase_next.
            // The current step does not get incremented until git_rebase_next (except on
            // the initial step), but we want to report the step that will be applied.
            long stepToApplyIndex = Proxy.git_rebase_operation_current(rebaseOperationHandle);

            stepToApplyIndex++;

            long totalStepCount = Proxy.git_rebase_operation_entrycount(rebaseOperationHandle);

            if (stepToApplyIndex == totalStepCount)
            {
                stepToApplyIndex = -1;
            }

            RebaseProgress progress = new RebaseProgress()
            {
                current = stepToApplyIndex,
                total = totalStepCount,
            };

            return progress;
        }

        private enum RebaseStepStatus
        {
            Committed,
            Conflicts,
            ChangesAlreadyApplied,
        }

        private class RebaseStepResult
        {
            public RebaseStepResult(RebaseStepStatus status)
            {
                Status = status;
                CommitId = GitOid.Empty;
            }

            public RebaseStepResult(RebaseStepStatus status, GitOid commitId)
            {
                Status = status;
                CommitId = commitId;
            }

            /// <summary>
            /// The ID of the commit that was generated, if any
            /// </summary>
            public GitOid CommitId;

            /// <summary>
            /// bool to indicate if the patch was already applied.
            /// If Patch was already applied, then CommitId will be empty (all zeros).
            /// </summary>
            public bool ChangesAlreadyApplied
            {
                get { return Status == RebaseStepStatus.ChangesAlreadyApplied; }
            }

            public RebaseStepStatus Status;
        }
    }
}
