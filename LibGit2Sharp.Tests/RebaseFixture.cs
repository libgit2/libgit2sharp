using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        const string topicBranch1PrimeName = "T1Prime";

        string filePathA = "a.txt";
        string filePathB = "b.txt";
        string filePathC = "c.txt";
        string filePathD = "d.txt";

        [Theory]
        [InlineData(topicBranch2Name, topicBranch2Name, topicBranch1Name, masterBranch1Name, 3)]
        [InlineData(topicBranch2Name, topicBranch2Name, topicBranch1Name, topicBranch1Name, 3)]
        [InlineData(topicBranch2Name, topicBranch1Name, masterBranch2Name, masterBranch2Name, 3)]
        [InlineData(topicBranch2Name, topicBranch1Name, masterBranch2Name, null, 3)]
        [InlineData(topicBranch1Name, null, masterBranch2Name, null, 3)]
        public void CanRebase(string initialBranchName,
                              string branchName,
                              string upstreamName,
                              string ontoName,
                              int stepCount)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                Commands.Checkout(repo, initialBranchName);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = (branchName == null) ? null : repo.Branches[branchName];
                Branch upstream = repo.Branches[upstreamName];
                Branch onto = (ontoName == null) ? null : repo.Branches[ontoName];
                Commit expectedSinceCommit = (branch == null) ? repo.Head.Tip : branch.Tip;
                Commit expectedUntilCommit = upstream.Tip;
                Commit expectedOntoCommit = (onto == null) ? upstream.Tip : onto.Tip;

                int beforeStepCallCount = 0;
                int afterStepCallCount = 0;
                bool beforeRebaseStepCountCorrect = true;
                bool afterRebaseStepCountCorrect = true;
                bool totalStepCountCorrect = true;

                List<Commit> PreRebaseCommits = new List<Commit>();
                List<CompletedRebaseStepInfo> PostRebaseResults = new List<CompletedRebaseStepInfo>();
                ObjectId expectedParentId = upstream.Tip.Id;

                RebaseOptions options = new RebaseOptions()
                {
                    RebaseStepStarting = x =>
                    {
                        beforeRebaseStepCountCorrect &= beforeStepCallCount == x.StepIndex;
                        totalStepCountCorrect &= (x.TotalStepCount == stepCount);
                        beforeStepCallCount++;
                        PreRebaseCommits.Add(x.StepInfo.Commit);
                    },
                    RebaseStepCompleted = x =>
                    {
                        afterRebaseStepCountCorrect &= (afterStepCallCount == x.CompletedStepIndex);
                        totalStepCountCorrect &= (x.TotalStepCount == stepCount);
                        afterStepCallCount++;
                        PostRebaseResults.Add(new CompletedRebaseStepInfo(x.Commit, x.WasPatchAlreadyApplied));
                    },
                };

                RebaseResult rebaseResult = repo.Rebase.Start(branch, upstream, onto, Constants.Identity, options);

                // Validation:
                Assert.True(afterRebaseStepCountCorrect, "Unexpected CompletedStepIndex value in RebaseStepCompleted");
                Assert.True(beforeRebaseStepCountCorrect, "Unexpected StepIndex value in RebaseStepStarting");
                Assert.True(totalStepCountCorrect, "Unexpected TotalStepcount value in Rebase step callback");
                Assert.Equal(RebaseStatus.Complete, rebaseResult.Status);
                Assert.Equal(stepCount, rebaseResult.TotalStepCount);
                Assert.Null(rebaseResult.CurrentStepInfo);

                Assert.Equal(stepCount, rebaseResult.CompletedStepCount);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Assert.Equal(stepCount, beforeStepCallCount);
                Assert.Equal(stepCount, afterStepCallCount);

                // Verify the chain of source commits that were rebased.
                CommitFilter sourceCommitFilter = new CommitFilter()
                {
                    IncludeReachableFrom = expectedSinceCommit,
                    ExcludeReachableFrom = expectedUntilCommit,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                Assert.Equal(repo.Commits.QueryBy(sourceCommitFilter), PreRebaseCommits);

                // Verify the chain of commits that resulted from the rebase.
                Commit expectedParent = expectedOntoCommit;
                foreach (CompletedRebaseStepInfo stepInfo in PostRebaseResults)
                {
                    Commit rebasedCommit = stepInfo.Commit;
                    Assert.Equal(expectedParent.Id, rebasedCommit.Parents.First().Id);
                    Assert.False(stepInfo.WasPatchAlreadyApplied);
                    expectedParent = rebasedCommit;
                }

                Assert.Equal(repo.Head.Tip, PostRebaseResults.Last().Commit);
            }
        }

        [Fact]
        public void CanRebaseBranchOntoItself()
        {
            // Maybe we should have an "up-to-date" return type for scenarios such as these,
            // but for now this test is to make sure we do something reasonable
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);
                Commands.Checkout(repo, topicBranch2Name);
                Branch b = repo.Branches[topicBranch2Name];

                RebaseResult result = repo.Rebase.Start(b, b, null, Constants.Identity, new RebaseOptions());
                Assert.Equal(0, result.TotalStepCount);
                Assert.Equal(RebaseStatus.Complete, result.Status);
                Assert.Equal(0, result.CompletedStepCount);
            }
        }

        private class CompletedRebaseStepInfo
        {
            public CompletedRebaseStepInfo(Commit commit, bool wasPatchAlreadyApplied)
            {
                Commit = commit;
                WasPatchAlreadyApplied = wasPatchAlreadyApplied;
            }

            public Commit Commit { get; set; }

            public bool WasPatchAlreadyApplied { get; set; }

            public override string ToString()
            {
                return string.Format("CompletedRebaseStepInfo: {0}", Commit);
            }
        }

        private class CompletedRebaseStepInfoEqualityComparer : IEqualityComparer<CompletedRebaseStepInfo>
        {
            bool IEqualityComparer<CompletedRebaseStepInfo>.Equals(CompletedRebaseStepInfo x, CompletedRebaseStepInfo y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if ((x == null && y != null) ||
                    (x != null && y == null))
                {
                    return false;
                }

                return x.WasPatchAlreadyApplied == y.WasPatchAlreadyApplied &&
                       ObjectId.Equals(x.Commit, y.Commit);
            }

            int IEqualityComparer<CompletedRebaseStepInfo>.GetHashCode(CompletedRebaseStepInfo obj)
            {
                int hashCode = obj.WasPatchAlreadyApplied.GetHashCode();

                if (obj.Commit != null)
                {
                    hashCode += obj.Commit.GetHashCode();
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Verify a single rebase, but in more detail.
        /// </summary>
        [Theory]
        [InlineData("* text=auto", "\r\n", new[] { "2cad6e96a0028f1764dcbde6292a9a1471acb114", "18fd3deebe6124b5dacc8426d589d617a968e8d1", "048977d8cb90d530e83cc615a17a49f3068f68c1" })]
        [InlineData("* text=auto", "\n", new[] { "2cad6e96a0028f1764dcbde6292a9a1471acb114", "18fd3deebe6124b5dacc8426d589d617a968e8d1", "048977d8cb90d530e83cc615a17a49f3068f68c1" })]
        [InlineData("* text=auto\n*.txt eol=lf", "\n", new[] { "577d176b00a55e88e9b34da87e4357dfc9a486fd", "ea0ad4d8b500394a61874ebfda5904376e2b1098", "521b8383ca3fde9e369587492e7a3945677f1b2c" })]
        [InlineData("* text=auto\r\n*.txt eol=crlf", "\r\n", new[] { "67d29fdf654ac4773c9405ab4b54aa7ff092f339", "7b70c02e175d378b44ea28aeeece775cd972047a", "81f203dbfe00a5c1ecd9c0e6b03705e6cffda5c0" })]
        [InlineData("* binary", "\r\n", new[] { "f5a5ded935597108709224170accddc5aeb5c287", "518adb8bb1ea1058d1825d3fe08d27f80c0e829b", "d2db503ab553c970d34e1b5e3ff68768adef05bc" })]
        [InlineData("* binary", "\n", new[] { "93a0e9680246d1f1e43fbd5308f7936424d9e81a", "5fd40bffbdd884632c330a254a2bd1dfaaaad3c1", "4df5c91b2d8318781b07d04f6bfa77304c372f1e" })]
        public void VerifyRebaseDetailed(string attributes, string lineEnding, string[] expectedIds)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);

            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo, attributes, lineEnding);

                Branch initialBranch = repo.Branches[topicBranch1Name];
                Branch upstreamBranch = repo.Branches[masterBranch2Name];

                Commands.Checkout(repo, initialBranch);
                Assert.False(repo.RetrieveStatus().IsDirty);

                bool wasCheckoutProgressCalled = false;
                bool wasCheckoutProgressCalledForResetingHead = false;
                bool wasCheckoutNotifyCalled = false;
                bool wasCheckoutNotifyCalledForResetingHead = false;

                bool startedApplyingSteps = false;

                RebaseOptions options = new RebaseOptions()
                {
                    OnCheckoutProgress = (x, y, z) =>
                    {
                        if (startedApplyingSteps)
                        {
                            wasCheckoutProgressCalled = true;
                        }
                        else
                        {
                            wasCheckoutProgressCalledForResetingHead = true;
                        }
                    },
                    OnCheckoutNotify = (x, y) =>
                    {
                        if (startedApplyingSteps)
                        {
                            wasCheckoutNotifyCalled = true;
                        }
                        else
                        {
                            wasCheckoutNotifyCalledForResetingHead = true;
                        }

                        return true;
                    },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,

                    RebaseStepStarting = x => startedApplyingSteps = true,

                };

                repo.Rebase.Start(null, upstreamBranch, null, Constants.Identity2, options);

                Assert.True(wasCheckoutNotifyCalledForResetingHead);
                Assert.True(wasCheckoutProgressCalledForResetingHead);
                Assert.True(wasCheckoutNotifyCalled);
                Assert.True(wasCheckoutProgressCalled);

                // Verify the chain of resultant rebased commits.
                CommitFilter commitFilter = new CommitFilter()
                {
                    IncludeReachableFrom = repo.Head.Tip,
                    ExcludeReachableFrom = upstreamBranch.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };

                List<ObjectId> expectedTreeIds = new List<ObjectId>()
                {
                    new ObjectId(expectedIds[0]),
                    new ObjectId(expectedIds[1]),
                    new ObjectId(expectedIds[2]),
                };

                List<Commit> rebasedCommits = repo.Commits.QueryBy(commitFilter).ToList();

                Assert.Equal(3, rebasedCommits.Count);
                for (int i = 0; i < 3; i++)
                {
                    Assert.Equal(expectedTreeIds[i], rebasedCommits[i].Tree.Id);
                    Assert.Equal(Constants.Signature.Name, rebasedCommits[i].Author.Name);
                    Assert.Equal(Constants.Signature.Email, rebasedCommits[i].Author.Email);
                    Assert.Equal(Constants.Signature2.Name, rebasedCommits[i].Committer.Name);
                    Assert.Equal(Constants.Signature2.Email, rebasedCommits[i].Committer.Email);
                }
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

                Commands.Checkout(repo, topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                int beforeStepCallCount = 0;
                int afterStepCallCount = 0;
                bool wasCheckoutProgressCalled = false;
                bool wasCheckoutNotifyCalled = false;

                RebaseOptions options = new RebaseOptions()
                {
                    RebaseStepStarting = x => beforeStepCallCount++,
                    RebaseStepCompleted = x => afterStepCallCount++,
                    OnCheckoutProgress = (x, y, z) => wasCheckoutProgressCalled = true,
                    OnCheckoutNotify = (x, y) => { wasCheckoutNotifyCalled = true; return true; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
                };

                RebaseResult rebaseResult = repo.Rebase.Start(branch, upstream, onto, Constants.Identity, options);

                // Verify that we have a conflict.
                Assert.Equal(CurrentOperation.RebaseMerge, repo.Info.CurrentOperation);
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                // Verify that expected callbacks were called
                Assert.Equal(1, beforeStepCallCount);
                Assert.Equal(0, afterStepCallCount);
                Assert.True(wasCheckoutProgressCalled, "CheckoutProgress callback was not called.");

                // Resolve the conflict
                foreach (Conflict conflict in repo.Index.Conflicts)
                {
                    Touch(repo.Info.WorkingDirectory,
                          conflict.Theirs.Path,
                          repo.Lookup<Blob>(conflict.Theirs.Id).GetContentText(new FilteringOptions(conflict.Theirs.Path)));
                    Commands.Stage(repo, conflict.Theirs.Path);
                }

                Assert.True(repo.Index.IsFullyMerged);

                // Clear the flags:
                wasCheckoutProgressCalled = false; wasCheckoutNotifyCalled = false;
                RebaseResult continuedRebaseResult = repo.Rebase.Continue(Constants.Identity, options);

                Assert.NotNull(continuedRebaseResult);
                Assert.Equal(RebaseStatus.Complete, continuedRebaseResult.Status);
                Assert.False(repo.RetrieveStatus().IsDirty);
                Assert.True(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                Assert.Equal(3, beforeStepCallCount);
                Assert.Equal(3, afterStepCallCount);
                Assert.True(wasCheckoutProgressCalled, "CheckoutProgress callback was not called.");
                Assert.True(wasCheckoutNotifyCalled, "CheckoutNotify callback was not called.");
            }
        }

        [Fact]
        public void ContinuingRebaseWithUnstagedChangesThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                Commands.Checkout(repo, topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseResult rebaseResult = repo.Rebase.Start(branch, upstream, onto, Constants.Identity, null);

                // Verify that we have a conflict.
                Assert.Equal(CurrentOperation.RebaseMerge, repo.Info.CurrentOperation);
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                Assert.Throws<UnmergedIndexEntriesException>(() =>
                    repo.Rebase.Continue(Constants.Identity, null));

                // Resolve the conflict
                foreach (Conflict conflict in repo.Index.Conflicts)
                {
                    Touch(repo.Info.WorkingDirectory,
                          conflict.Theirs.Path,
                          repo.Lookup<Blob>(conflict.Theirs.Id).GetContentText(new FilteringOptions(conflict.Theirs.Path)));
                    Commands.Stage(repo, conflict.Theirs.Path);
                }

                Touch(repo.Info.WorkingDirectory,
                    filePathA,
                    "Unstaged content");

                Assert.Throws<UnmergedIndexEntriesException>(() =>
                    repo.Rebase.Continue(Constants.Identity, null));

                Assert.True(repo.Index.IsFullyMerged);
            }
        }

        [Fact]
        public void CanSpecifyFileConflictStrategy()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                Commands.Checkout(repo, topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseOptions options = new RebaseOptions()
                {
                    FileConflictStrategy = CheckoutFileConflictStrategy.Ours,
                };

                RebaseResult rebaseResult = repo.Rebase.Start(branch, upstream, onto, Constants.Identity, options);

                // Verify that we have a conflict.
                Assert.Equal(CurrentOperation.RebaseMerge, repo.Info.CurrentOperation);
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                string conflictFile = filePathB;
                // Get the information on the conflict.
                Conflict conflict = repo.Index.Conflicts[conflictFile];

                Assert.NotNull(conflict);
                Assert.NotNull(conflict.Theirs);
                Assert.NotNull(conflict.Ours);

                Blob expectedBlob = repo.Lookup<Blob>(conflict.Ours.Id);

                // Check the content of the file on disk matches what is expected.
                string expectedContent = expectedBlob.GetContentText(new FilteringOptions(conflictFile));
                Assert.Equal(expectedContent, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, conflictFile)));
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

                Commands.Checkout(repo, topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseResult rebaseResult = repo.Rebase.Start(branch, upstream, onto, Constants.Identity, null);

                // Verify that we have a conflict.
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                RebaseStepInfo info = repo.Rebase.GetCurrentStepInfo();

                Assert.Equal(0, repo.Rebase.GetCurrentStepIndex());
                Assert.Equal(3, repo.Rebase.GetTotalStepCount());
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

                Commands.Checkout(repo, topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseResult rebaseResult = repo.Rebase.Start(branch, upstream, onto, Constants.Identity, null);

                // Verify that we have a conflict.
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(0, rebaseResult.CompletedStepCount);
                Assert.Equal(3, rebaseResult.TotalStepCount);

                // Set up the callbacks to verify that checkout progress / notify
                // callbacks are called.
                bool wasCheckoutProgressCalled = false;
                bool wasCheckoutNotifyCalled = false;
                RebaseOptions options = new RebaseOptions()
                {
                    OnCheckoutProgress = (x, y, z) => wasCheckoutProgressCalled = true,
                    OnCheckoutNotify = (x, y) => { wasCheckoutNotifyCalled = true; return true; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
                };

                repo.Rebase.Abort(options);
                Assert.False(repo.RetrieveStatus().IsDirty, "Repository workdir is dirty after Rebase.Abort.");
                Assert.True(repo.Index.IsFullyMerged, "Repository index is not fully merged after Rebase.Abort.");
                Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);

                Assert.True(wasCheckoutProgressCalled, "Checkout progress callback was not called during Rebase.Abort.");
                Assert.True(wasCheckoutNotifyCalled, "Checkout notify callback was not called during Rebase.Abort.");
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

                Commands.Checkout(repo, topicBranch1Name);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[topicBranch1Name];
                Branch upstream = repo.Branches[conflictBranch1Name];
                Branch onto = repo.Branches[conflictBranch1Name];

                RebaseResult rebaseResult = repo.Rebase.Start(branch, upstream, onto, Constants.Identity, null);

                // Verify that we have a conflict.
                Assert.Equal(RebaseStatus.Conflicts, rebaseResult.Status);
                Assert.True(repo.RetrieveStatus().IsDirty);
                Assert.Equal(CurrentOperation.RebaseMerge, repo.Info.CurrentOperation);

                Assert.Throws<LibGit2SharpException>(() =>
                    repo.Rebase.Start(branch, upstream, onto, Constants.Identity, null));
            }
        }

        [Fact]
        public void RebaseOperationsWithoutRebasingThrow()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);

                Commands.Checkout(repo, topicBranch1Name);

                Assert.Throws<NotFoundException>(() =>
                    repo.Rebase.Continue(Constants.Identity, new RebaseOptions()));

                Assert.Throws<NotFoundException>(() =>
                    repo.Rebase.Abort());
            }
        }

        [Fact]
        public void CurrentStepInfoIsNullWhenNotRebasing()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo);
                Commands.Checkout(repo, topicBranch1Name);

                Assert.Null(repo.Rebase.GetCurrentStepInfo());
            }
        }

        [Theory]
        [InlineData("* text=auto", "\r\n", "379e80ed7824be7672e1e30ddd8f44aa081d57d4")]
        [InlineData("* text=auto", "\n", "379e80ed7824be7672e1e30ddd8f44aa081d57d4")]
        [InlineData("* text=auto\n*.txt eol=lf", "\n", "94121eeebf7cfe0acf22425eab36fcdc737132b6")]
        [InlineData("* text=auto\r\n*.txt eol=crlf", "\r\n", "dad06142cc632aea81cbc8486583011c4d622580")]
        [InlineData("* binary", "\r\n", "44492d98b725189cfc0203d4192dfbb1fd34bf02")]
        [InlineData("* binary", "\n", "f4b5b95de77f4cd97b4728617bae2dd8ba9af914")]
        public void CanRebaseHandlePatchAlreadyApplied(string attributes, string lineEnding, string expectedShaText)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (Repository repo = new Repository(path))
            {
                ConstructRebaseTestRepository(repo, attributes, lineEnding);

                Commands.Checkout(repo, topicBranch1Name);

                Branch topicBranch1Prime = repo.CreateBranch(topicBranch1PrimeName, masterBranch1Name);

                string newFileRelativePath = "new_file.txt";
                Touch(repo.Info.WorkingDirectory, newFileRelativePath, "New Content");
                Commands.Stage(repo, newFileRelativePath);
                Commit commit = repo.Commit("new commit 1", Constants.Signature, Constants.Signature, new CommitOptions());

                Commands.Checkout(repo, topicBranch1Prime);
                var cherryPickResult = repo.CherryPick(commit, Constants.Signature2);
                Assert.Equal(CherryPickStatus.CherryPicked, cherryPickResult.Status);

                string newFileRelativePath2 = "new_file_2.txt";
                Touch(repo.Info.WorkingDirectory, newFileRelativePath2, "New Content for path 2");
                Commands.Stage(repo, newFileRelativePath2);
                repo.Commit("new commit 2", Constants.Signature, Constants.Signature, new CommitOptions());

                Branch upstreamBranch = repo.Branches[topicBranch1Name];

                List<CompletedRebaseStepInfo> rebaseResults = new List<CompletedRebaseStepInfo>();

                RebaseOptions options = new RebaseOptions()
                {
                    RebaseStepCompleted = x =>
                    {
                        rebaseResults.Add(new CompletedRebaseStepInfo(x.Commit, x.WasPatchAlreadyApplied));
                    }
                };

                repo.Rebase.Start(null, upstreamBranch, null, Constants.Identity2, options);
                ObjectId secondCommitExpectedTreeId = new ObjectId(expectedShaText);
                Signature secondCommitAuthorSignature = Constants.Signature;
                Identity secondCommitCommiterIdentity = Constants.Identity2;

                Assert.Equal(2, rebaseResults.Count);
                Assert.True(rebaseResults[0].WasPatchAlreadyApplied);

                Assert.False(rebaseResults[1].WasPatchAlreadyApplied);
                Assert.NotNull(rebaseResults[1].Commit);

                // This is the expected tree ID of the new commit.
                Assert.Equal(secondCommitExpectedTreeId, rebaseResults[1].Commit.Tree.Id);
                Assert.True(Signature.Equals(secondCommitAuthorSignature, rebaseResults[1].Commit.Author));
                Assert.Equal<string>(secondCommitCommiterIdentity.Name, rebaseResults[1].Commit.Committer.Name, StringComparer.Ordinal);
                Assert.Equal<string>(secondCommitCommiterIdentity.Email, rebaseResults[1].Commit.Committer.Email, StringComparer.Ordinal);
            }
        }

        [Fact]
        public void RebasingInBareRepositoryThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch rebaseUpstreamBranch = repo.Branches["refs/heads/test"];

                Assert.NotNull(rebaseUpstreamBranch);
                Assert.Throws<BareRepositoryException>(() => repo.Rebase.Start(null, rebaseUpstreamBranch, null, Constants.Identity, new RebaseOptions()));
                Assert.Throws<BareRepositoryException>(() => repo.Rebase.Continue(Constants.Identity, new RebaseOptions()));
                Assert.Throws<BareRepositoryException>(() => repo.Rebase.Abort());
            }
        }

        private void ConstructRebaseTestRepository(Repository repo, string attributes = "* text=auto", string lineEnding = "\r\n")
        {
            // Constructs a graph that looks like:
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
            const string fileContentA1 = "A1";

            const string fileContentB1 = "B1";
            const string fileContentB2 = "B2";
            const string fileContentB3 = "B3";
            const string fileContentB4 = "B4";

            const string fileContentC1 = "C1";
            const string fileContentC2 = "C2";
            const string fileContentC3 = "C3";
            const string fileContentC4 = "C4";

            const string fileContentD1 = "D1";
            const string fileContentD2 = "D2";
            const string fileContentD3 = "D3";

            string workdir = repo.Info.WorkingDirectory;
            Commit commit = null;

            CreateAttributesFile(repo, attributes);

            Commands.Stage(repo, ".gitattributes");
            commit = repo.Commit("setup", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathA, fileContentA1);
            Commands.Stage(repo, filePathA);
            commit = repo.Commit("commit 1", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathB, fileContentB1);
            Commands.Stage(repo, filePathB);
            commit = repo.Commit("commit 2", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathC, fileContentC1);
            Commands.Stage(repo, filePathC);
            commit = repo.Commit("commit 3", Constants.Signature, Constants.Signature, new CommitOptions());

            Branch masterBranch1 = repo.CreateBranch(masterBranch1Name, commit);

            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2));
            Commands.Stage(repo, filePathB);
            commit = repo.Commit("commit 4", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2, fileContentB3));
            Commands.Stage(repo, filePathB);
            commit = repo.Commit("commit 5", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2, fileContentB3, fileContentB4));
            Commands.Stage(repo, filePathB);
            commit = repo.Commit("commit 6", Constants.Signature, Constants.Signature, new CommitOptions());

            repo.CreateBranch(topicBranch1Name, commit);

            Touch(workdir, filePathC, string.Join(lineEnding, fileContentC1, fileContentC2));
            Commands.Stage(repo, filePathC);
            commit = repo.Commit("commit 7", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathC, string.Join(lineEnding, fileContentC1, fileContentC2, fileContentC3));
            Commands.Stage(repo, filePathC);
            commit = repo.Commit("commit 8", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathC, string.Join(lineEnding, fileContentC1, fileContentC2, fileContentC3, fileContentC4));
            Commands.Stage(repo, filePathC);
            commit = repo.Commit("commit 9", Constants.Signature, Constants.Signature, new CommitOptions());

            repo.CreateBranch(topicBranch2Name, commit);

            Commands.Checkout(repo, masterBranch1.Tip);
            Touch(workdir, filePathD, fileContentD1);
            Commands.Stage(repo, filePathD);
            commit = repo.Commit("commit 10", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathD, string.Join(lineEnding, fileContentD1, fileContentD2));
            Commands.Stage(repo, filePathD);
            commit = repo.Commit("commit 11", Constants.Signature, Constants.Signature, new CommitOptions());

            Touch(workdir, filePathD, string.Join(lineEnding, fileContentD1, fileContentD2, fileContentD3));
            Commands.Stage(repo, filePathD);
            commit = repo.Commit("commit 12", Constants.Signature, Constants.Signature, new CommitOptions());

            repo.CreateBranch(masterBranch2Name, commit);

            // Create commit / branch that conflicts with T1 and T2
            Touch(workdir, filePathB, string.Join(lineEnding, fileContentB1, fileContentB2 + fileContentB3 + fileContentB4));
            Commands.Stage(repo, filePathB);
            commit = repo.Commit("commit 13", Constants.Signature, Constants.Signature, new CommitOptions());
            repo.CreateBranch(conflictBranch1Name, commit);
        }
    }
}
