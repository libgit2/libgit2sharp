using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RebaseFixture : BaseFixture
    {
        const string masterBranch1Name = "M1";
        const string masterBranch2Name = "M2";
        const string topicBranch1Name = "T1";
        const string topicBranch2Name = "T2";
        const string conflictBranch1Name = "C1";

        [Theory]
        [InlineData(topicBranch2Name, topicBranch2Name, topicBranch1Name, masterBranch1Name, 3)]
        [InlineData(topicBranch2Name, topicBranch2Name, topicBranch1Name, topicBranch1Name, 3)]
        [InlineData(topicBranch2Name, topicBranch1Name, masterBranch2Name, masterBranch2Name, 3)]
        [InlineData(topicBranch2Name, topicBranch1Name, masterBranch2Name, null, 3)]
        [InlineData(topicBranch1Name, null, masterBranch2Name, null, 3)]
        public void CanRebase(string initialBranchName, string branchName, string upstreamName, string ontoName, int stepCount)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                repo.Checkout(initialBranchName);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = (branchName == null) ? null : repo.Branches[branchName];
                Branch upstream = (upstreamName == null) ? null : repo.Branches[upstreamName];
                Branch onto = (ontoName == null) ? null : repo.Branches[ontoName];

                int beforeStepCallCount = 0;
                int afterStepCallCount = 0;
                RebaseOptions options = new RebaseOptions()
                {
                    RebaseStepStarting = x => beforeStepCallCount++,
                    RebaseStepCompleted = x => afterStepCallCount++,
                };

                RebaseResult rebaseResult = repo.Rebase(branch, upstream, onto, Constants.Signature, options);

                // Validation:
                Assert.Equal(RebaseStatus.Complete, rebaseResult.Status);
                Assert.Equal(stepCount, rebaseResult.TotalStepCount);
                Assert.Null(rebaseResult.CurrentStepInfo);

                // What is the "current step" of a completed operation?
                // it looks like it is total steps - 1
                Assert.Equal(stepCount, rebaseResult.CompletedStepCount);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Assert.Equal(stepCount, beforeStepCallCount);
                Assert.Equal(stepCount, afterStepCallCount);

                // TODO: Validate the expected HEAD commit ID
            }
        }

        [Fact]
        public void CanContinueRebase()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                repo.Checkout(topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                int beforeStepCallCount = 0;
                int afterStepCallCount = 0;
                RebaseOptions options = new RebaseOptions()
                {
                    RebaseStepStarting = x => beforeStepCallCount++,
                    RebaseStepCompleted = x => afterStepCallCount++,
                };

                RebaseResult rebaseResult = repo.Rebase(branch, upstream, onto, Constants.Signature, options);

                // Verify that we have a conflict.
                Assert.Equal(CurrentOperation.RebaseMerge, repo.Info.CurrentOperation);
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                Assert.Equal(1, beforeStepCallCount);
                Assert.Equal(0, afterStepCallCount);

                // Resolve the conflict
                foreach (Conflict conflict in repo.Index.Conflicts)
                {
                    Touch(repo.Info.WorkingDirectory,
                          conflict.Theirs.Path,
                          repo.Lookup<Blob>(conflict.Theirs.Id).GetContentStream(new FilteringOptions(conflict.Theirs.Path)));
                    repo.Stage(conflict.Theirs.Path);
                }

                Assert.True(repo.Index.IsFullyMerged);

                RebaseResult continuedRebaseResult = repo.CurrentRebaseOperation.Continue(Constants.Signature, options);

                Assert.NotNull(continuedRebaseResult);
                Assert.Equal(RebaseStatus.Complete, continuedRebaseResult.Status);
                Assert.False(repo.RetrieveStatus().IsDirty);
                Assert.True(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                Assert.Equal(3, beforeStepCallCount);
                Assert.Equal(3, afterStepCallCount);

                // TODO: Validate the expected HEAD commit ID
            }
        }

        [Fact]
        public void CanQueryRebaseOperation()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                repo.Checkout(topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseResult rebaseResult = repo.Rebase(branch, upstream, onto, Constants.Signature, null);

                // Verify that we have a conflict.
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                RebaseStepInfo info = repo.CurrentRebaseOperation.CurrentStepInfo;
                Assert.Equal(0, info.StepIndex);
                Assert.Equal(3, info.TotalStepCount);
                Assert.Equal(RebaseStepOperation.Pick, info.Type);
            }
        }

        [Fact]
        public void CanAbortRebase()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                repo.Checkout(topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseResult rebaseResult = repo.Rebase(branch, upstream, onto, Constants.Signature, null);

                // Verify that we have a conflict.
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                repo.CurrentRebaseOperation.Abort();
                Assert.False(repo.RetrieveStatus().IsDirty);
                Assert.True(repo.Index.IsFullyMerged);
                Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);
            }
        }

        [Fact]
        public void RebaseWhileAlreadyRebasingThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                repo.Checkout(topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseResult rebaseResult = repo.Rebase(branch, upstream, onto, Constants.Signature, null);

                // Verify that we have a conflict.
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.Equal(CurrentOperation.RebaseMerge, repo.Info.CurrentOperation);

                Assert.Throws<LibGit2SharpException>(() =>
                    repo.Rebase(branch, upstream, onto, Constants.Signature, null));
                }
        }

        [Fact]
        public void CurrentRebaseOperationIsNullWhenNotRebasing()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);
                repo.Checkout(topicBranch1Name);

                Assert.Null(repo.CurrentRebaseOperation);
            }
        }

        private void ConstructRebaseTestRepository(Repository repo)
        {
            //                         * -- * -- *   (modifications to c.txt)
            //                        /          |
            //                       /           T2
            //                      /
            //           * -- * -- *                 (modifications to b.txt)
            //          /          |
            //         /           T1
            //        /
            // *--*--*--*--*--*----
            //       |        |    \
            //       M1       M2    \
            //                       ---*
            //                          |
            //                          C1
            const string lineEnding = "\r\n";

            string filePathA = "a.txt";
            const string fileContentA1 = "A1";
            // const string fileContentA2 = "A2";
            // const string fileContentA3 = "A3";

            string filePathB = "b.txt";
            const string fileContentB1 = "B1";
            const string fileContentB2 = "B2";
            const string fileContentB3 = "B3";
            const string fileContentB4 = "B4";

            string filePathC = "c.txt";
            const string fileContentC1 = "C1";
            const string fileContentC2 = "C2";
            const string fileContentC3 = "C3";
            const string fileContentC4 = "C4";

            string filePathD = "d.txt";
            const string fileContentD1 = "D1";
            const string fileContentD2 = "D2";
            const string fileContentD3 = "D3";

            string workdir = repo.Info.WorkingDirectory;
            Commit commit = null;

            Touch(workdir, filePathA, fileContentA1);
            repo.Stage(filePathA);
            commit = repo.Commit("commit 1", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathB, fileContentB1);
            repo.Stage(filePathB);
            commit = repo.Commit("commit 2", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathC, fileContentC1);
            repo.Stage(filePathC);
            commit = repo.Commit("commit 3", Constants.Signature, Constants.Signature, new CommitOptions());

            repo.CreateBranch(masterBranch1Name, commit, Constants.Signature);

            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2));
            repo.Stage(filePathB);
            commit = repo.Commit("commit 4", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2, fileContentB3));
            repo.Stage(filePathB);
            commit = repo.Commit("commit 5", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2, fileContentB3, fileContentB4));
            repo.Stage(filePathB);
            commit = repo.Commit("commit 6", Constants.Signature, Constants.Signature, new CommitOptions());

            repo.CreateBranch(topicBranch1Name, commit, Constants.Signature);

            Touch(workdir, filePathC, string.Join(lineEnding, fileContentC1, fileContentC2));
            repo.Stage(filePathC);
            commit = repo.Commit("commit 7", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathC, string.Join(lineEnding, fileContentC1, fileContentC2, fileContentC3));
            repo.Stage(filePathC);
            commit = repo.Commit("commit 8", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathC, string.Join(lineEnding, fileContentC1, fileContentC2, fileContentC3, fileContentC4));
            repo.Stage(filePathC);
            commit = repo.Commit("commit 9", Constants.Signature, Constants.Signature, new CommitOptions());

            repo.CreateBranch(topicBranch2Name, commit, Constants.Signature);

            repo.Checkout(masterBranch1Name);
            Touch(workdir, filePathD, fileContentD1);
            repo.Stage(filePathD);
            commit = repo.Commit("commit 10", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathD, string.Join(lineEnding, fileContentD1, fileContentD2));
            repo.Stage(filePathD);
            commit = repo.Commit("commit 11", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathD, string.Join(lineEnding, fileContentD1, fileContentD2, fileContentD3));
            repo.Stage(filePathD);
            commit = repo.Commit("commit 12", Constants.Signature, Constants.Signature, new CommitOptions());

            repo.CreateBranch(masterBranch2Name, commit, Constants.Signature);

            // Create commit / branch that conflicts with T1 and T2
            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2 + fileContentB3 + fileContentB4));
            repo.Stage(filePathB);
            commit = repo.Commit("commit 13", Constants.Signature, Constants.Signature, new CommitOptions());
            repo.CreateBranch(conflictBranch1Name, commit, Constants.Signature);
        }
    }
}
