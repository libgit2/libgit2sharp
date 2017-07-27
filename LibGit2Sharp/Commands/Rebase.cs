using System;
using LibGit2Sharp;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    public static partial class Commands
    {
        /// <summary>
        /// Begin, continue or abort a rebase operation in a repository.
        /// </summary>
        public static class Rebase
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

                // <summary>
                // No commit to cherry-pick. Run the given command and continue
                // if successful.
                // </summary>
                // Exec
            }

            private unsafe static AnnotatedCommitHandle AnnotatedCommitHandleFromRefHandle(Repository repository, ReferenceHandle refHandle)
            {
                return (refHandle == null) ?
                    new AnnotatedCommitHandle(null, false) :
                    Proxy.git_annotated_commit_from_ref(repository.Handle, refHandle);
            }

            /// <summary>
            /// Start a rebase operation.
            /// </summary>
            /// <param name="repository">The <see cref="Repository"/> to start a rebase in.</param>
            /// <param name="branch">The branch to rebase.</param>
            /// <param name="upstream">The starting commit to rebase.</param>
            /// <param name="onto">The branch to rebase onto.</param>
            /// <param name="committer">The <see cref="Identity"/> of who added the change to the repository.</param>
            /// <param name="options">The <see cref="RebaseOptions"/> that specify the rebase behavior.</param>
            /// <returns>true if completed successfully, false if conflicts encountered.</returns>
            public static RebaseResult Start(Repository repository, Branch branch, Branch upstream, Branch onto, Identity committer, RebaseOptions options)
            {
                Ensure.ArgumentNotNull(upstream, "upstream");

                options = options ?? new RebaseOptions();

                EnsureNonBareRepo(repository);

                if (repository.Info.CurrentOperation != CurrentOperation.None)
                {
                    throw new LibGit2SharpException("A {0} operation is already in progress.",
                        repository.Info.CurrentOperation);
                }

                Func<Branch, ReferenceHandle> RefHandleFromBranch = (Branch b) =>
                {
                    return (b == null) ?
                        null :
                        repository.Refs.RetrieveReferencePtr(b.CanonicalName);
                };

                using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
                {
                    GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                    {
                        version = 1,
                        checkout_options = checkoutOptionsWrapper.Options,
                    };

                    using (ReferenceHandle branchRefPtr = RefHandleFromBranch(branch))
                    using (ReferenceHandle upstreamRefPtr = RefHandleFromBranch(upstream))
                    using (ReferenceHandle ontoRefPtr = RefHandleFromBranch(onto))
                    using (AnnotatedCommitHandle annotatedBranchCommitHandle = AnnotatedCommitHandleFromRefHandle(repository, branchRefPtr))
                    using (AnnotatedCommitHandle upstreamRefAnnotatedCommitHandle = AnnotatedCommitHandleFromRefHandle(repository, upstreamRefPtr))
                    using (AnnotatedCommitHandle ontoRefAnnotatedCommitHandle = AnnotatedCommitHandleFromRefHandle(repository, ontoRefPtr))
                    using (RebaseHandle rebaseOperationHandle = Proxy.git_rebase_init(repository.Handle,
                        annotatedBranchCommitHandle,
                        upstreamRefAnnotatedCommitHandle,
                        ontoRefAnnotatedCommitHandle,
                        gitRebaseOptions))
                    {
                        RebaseResult rebaseResult = RebaseOperationImpl.Run(rebaseOperationHandle,
                            repository,
                            committer,
                            options);
                        return rebaseResult;
                    }
                }
            }

            /// <summary>
            /// Continue the current rebase.
            /// </summary>
            /// <param name="repository">The <see cref="Repository"/> to continue a rebase in.</param>
            /// <param name="committer">The <see cref="Identity"/> of who added the change to the repository.</param>
            /// <param name="options">The <see cref="RebaseOptions"/> that specify the rebase behavior.</param>
            public static unsafe RebaseResult Continue(Repository repository, Identity committer, RebaseOptions options)
            {
                Ensure.ArgumentNotNull(committer, "committer");

                options = options ?? new RebaseOptions();

                EnsureNonBareRepo(repository);

                using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
                {
                    GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                    {
                        version = 1,
                        checkout_options = checkoutOptionsWrapper.Options,
                    };

                    using (RebaseHandle rebase = Proxy.git_rebase_open(repository.Handle, gitRebaseOptions))
                    {
                        // TODO: Should we check the pre-conditions for committing here
                        // for instance - what if we had failed on the git_rebase_finish call,
                        // do we want continue to be able to restart afterwords...
                        var rebaseCommitResult = Proxy.git_rebase_commit(rebase, null, committer);

                        // Report that we just completed the step
                        if (options.RebaseStepCompleted != null)
                        {
                            // Get information on the current step
                            long currentStepIndex = Proxy.git_rebase_operation_current(rebase);
                            long totalStepCount = Proxy.git_rebase_operation_entrycount(rebase);
                            git_rebase_operation* gitRebasestepInfo = Proxy.git_rebase_operation_byindex(rebase, currentStepIndex);

                            var stepInfo = new RebaseStepInfo(gitRebasestepInfo->type,
                                                              repository.Lookup<Commit>(ObjectId.BuildFromPtr(&gitRebasestepInfo->id)),
                                                              LaxUtf8NoCleanupMarshaler.FromNative(gitRebasestepInfo->exec));

                            if (rebaseCommitResult.WasPatchAlreadyApplied)
                            {
                                options.RebaseStepCompleted(new AfterRebaseStepInfo(stepInfo, currentStepIndex, totalStepCount));
                            }
                            else
                            {
                                options.RebaseStepCompleted(new AfterRebaseStepInfo(stepInfo,
                                                                                    repository.Lookup<Commit>(new ObjectId(rebaseCommitResult.CommitId)),
                                                                                    currentStepIndex,
                                                                                    totalStepCount));
                            }
                        }

                        RebaseResult rebaseResult = RebaseOperationImpl.Run(rebase, repository, committer, options);
                        return rebaseResult;
                    }
                }
            }

            /// <summary>
            /// Abort the rebase operation.
            /// </summary>
            /// <param name="repository">The <see cref="Repository"/> to abort a rebase in.</param>
            public static void Abort(Repository repository)
            {
                Abort(repository, null);
            }

            /// <summary>
            /// Abort the rebase operation.
            /// </summary>
            /// <param name="repository">The <see cref="Repository"/> to abort a rebase in.</param>
            /// <param name="options">The <see cref="RebaseOptions"/> that specify the rebase behavior.</param>
            public static void Abort(Repository repository, RebaseOptions options)
            {
                options = options ?? new RebaseOptions();

                EnsureNonBareRepo(repository);

                using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
                {
                    GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                    {
                        checkout_options = checkoutOptionsWrapper.Options,
                    };

                    using (RebaseHandle rebase = Proxy.git_rebase_open(repository.Handle, gitRebaseOptions))
                    {
                        Proxy.git_rebase_abort(rebase);
                    }
                }
            }

            /// <summary>
            /// The info on the current step.
            /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
            /// </summary>
            public static unsafe RebaseStepInfo GetCurrentStepInfo(Repository repository)
            {
                if (repository.Info.CurrentOperation != LibGit2Sharp.CurrentOperation.RebaseMerge)
                {
                    return null;
                }

                GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                {
                    version = 1,
                };

                using (RebaseHandle rebaseHandle = Proxy.git_rebase_open(repository.Handle, gitRebaseOptions))
                {
                    long currentStepIndex = Proxy.git_rebase_operation_current(rebaseHandle);
                    git_rebase_operation* gitRebasestepInfo = Proxy.git_rebase_operation_byindex(rebaseHandle, currentStepIndex);
                    var stepInfo = new RebaseStepInfo(gitRebasestepInfo->type,
                                                      repository.Lookup<Commit>(ObjectId.BuildFromPtr(&gitRebasestepInfo->id)),
                                                      LaxUtf8Marshaler.FromNative(gitRebasestepInfo->exec));
                    return stepInfo;
                }
            }

            /// <summary>
            /// Get info on the specified step
            /// </summary>
            /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
            /// <param name="stepIndex">The step number to get information about.</param>
            /// <returns></returns>
            public static unsafe RebaseStepInfo GetStepInfo(Repository repository, long stepIndex)
            {
                if (repository.Info.CurrentOperation != LibGit2Sharp.CurrentOperation.RebaseMerge)
                {
                    return null;
                }

                GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                {
                    version = 1,
                };

                using (RebaseHandle rebaseHandle = Proxy.git_rebase_open(repository.Handle, gitRebaseOptions))
                {
                    git_rebase_operation* gitRebasestepInfo = Proxy.git_rebase_operation_byindex(rebaseHandle, stepIndex);
                    var stepInfo = new RebaseStepInfo(gitRebasestepInfo->type,
                                                      repository.Lookup<Commit>(ObjectId.BuildFromPtr(&gitRebasestepInfo->id)),
                                                      LaxUtf8Marshaler.FromNative(gitRebasestepInfo->exec));
                    return stepInfo;
                }
            }

            /// <summary>
            /// Get the index of the current step in the rebase process.
            /// </summary>
            /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
            /// <returns>The index</returns>
            public static long GetCurrentStepIndex(Repository repository)
            {
                GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                {
                    version = 1,
                };

                using (RebaseHandle rebaseHandle = Proxy.git_rebase_open(repository.Handle, gitRebaseOptions))
                {
                    return Proxy.git_rebase_operation_current(rebaseHandle);
                }
            }

            /// <summary>
            /// Get the number of steps in the rebase process.
            /// </summary>
            /// <param name="repository">The <see cref="Repository"/> to get the information about.</param>
            /// <returns>The number of steps in the rebase operation.</returns>
            public static long GetTotalStepCount(Repository repository)
            {
                GitRebaseOptions gitRebaseOptions = new GitRebaseOptions()
                {
                    version = 1,
                };

                using (RebaseHandle rebaseHandle = Proxy.git_rebase_open(repository.Handle, gitRebaseOptions))
                {
                    return Proxy.git_rebase_operation_entrycount(rebaseHandle);
                }
            }

            private static void EnsureNonBareRepo(Repository repository)
            {
                if (repository.Info.IsBare)
                {
                    throw new BareRepositoryException("Rebase operations in a bare repository are not supported.");
                }
            }
        }
    }
}
