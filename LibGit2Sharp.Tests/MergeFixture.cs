using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class MergeFixture : BaseFixture
    {
        [Fact]
        public void ANewRepoIsFullyMerged()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.True(repo.Index.IsFullyMerged);
            }
        }

        [Fact]
        public void AFullyMergedRepoOnlyContainsStagedIndexEntries()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.True(repo.Index.IsFullyMerged);

                foreach (var entry in repo.Index)
                {
                    Assert.Equal(StageLevel.Staged, entry.StageLevel);
                }
            }
        }

        [Fact]
        public void SoftResetARepoWithUnmergedEntriesThrows()
        {
            var path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.False(repo.Index.IsFullyMerged);

                var headCommit = repo.Head.Tip;
                var firstCommitParent = headCommit.Parents.First();
                Assert.Throws<UnmergedIndexEntriesException>(
                    () => repo.Reset(ResetMode.Soft, firstCommitParent));
            }
        }

        [Fact]
        public void CommitAgainARepoWithUnmergedEntriesThrows()
        {
            var path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.False(repo.Index.IsFullyMerged);

                var author = Constants.Signature;
                Assert.Throws<UnmergedIndexEntriesException>(
                    () => repo.Commit("Try commit unmerged entries", author, author));
            }
        }

        [Fact]
        public void CanRetrieveTheBranchBeingMerged()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string firstBranch = "9fd738e8f7967c078dceed8190330fc8648ee56a";
                const string secondBranch = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";

                Touch(repo.Info.Path, "MERGE_HEAD", string.Format("{0}{1}{2}{1}", firstBranch, "\n", secondBranch));

                Assert.Equal(CurrentOperation.Merge, repo.Info.CurrentOperation);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanMergeRepoNonFastForward(bool shouldMergeOccurInDetachedHeadState)
        {
            const string firstBranchFileName = "first branch file.txt";
            const string secondBranchFileName = "second branch file.txt";
            const string sharedBranchFileName = "first+second branch file.txt";

            string path = SandboxStandardTestRepo();

            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                Commands.Checkout(repo, firstBranch);
                var originalTreeCount = firstBranch.Tip.Tree.Count;

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");
                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);

                if (shouldMergeOccurInDetachedHeadState)
                {
                    // Detaches HEAD
                    Commands.Checkout(repo, secondBranch.Tip);
                }
                else
                {
                    Commands.Checkout(repo, secondBranch);
                }

                // Commit with ONE new file to second branch (FirstBranch and SecondBranch now point to separate commits that both have the same parent commit).
                AddFileCommitToRepo(repo, secondBranchFileName);

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.NonFastForward, mergeResult.Status);

                Assert.Equal(repo.Head.Tip, mergeResult.Commit);
                Assert.Equal(originalTreeCount + 3, mergeResult.Commit.Tree.Count);    // Expecting original tree count plussed by the 3 added files.
                Assert.Equal(2, mergeResult.Commit.Parents.Count());   // Merge commit should have 2 parents
                Assert.Equal(shouldMergeOccurInDetachedHeadState, repo.Info.IsHeadDetached);

                if (!shouldMergeOccurInDetachedHeadState)
                {
                    // Ensure HEAD is still attached and points to SecondBranch
                    Assert.Equal(repo.Refs.Head.TargetIdentifier, secondBranch.CanonicalName);
                }
            }
        }

        [Fact]
        public void IsUpToDateMerge()
        {
            const string sharedBranchFileName = "first+second branch file.txt";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                Commands.Checkout(repo, firstBranch);

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");

                Commands.Checkout(repo, secondBranch);

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.UpToDate, mergeResult.Status);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanFastForwardRepos(bool shouldMergeOccurInDetachedHeadState)
        {
            const string firstBranchFileName = "first branch file.txt";
            const string sharedBranchFileName = "first+second branch file.txt";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Reset the index and the working tree.
                repo.Reset(ResetMode.Hard);

                // Clean the working directory.
                repo.RemoveUntrackedFiles();

                var firstBranch = repo.CreateBranch("FirstBranch");
                Commands.Checkout(repo, firstBranch);

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");

                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);

                if (shouldMergeOccurInDetachedHeadState)
                {
                    // Detaches HEAD
                    Commands.Checkout(repo, secondBranch.Tip);
                }
                else
                {
                    Commands.Checkout(repo, secondBranch);
                }

                Assert.Equal(shouldMergeOccurInDetachedHeadState, repo.Info.IsHeadDetached);

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.FastForward, mergeResult.Status);
                Assert.Equal(repo.Branches["FirstBranch"].Tip, mergeResult.Commit);
                Assert.Equal(repo.Branches["FirstBranch"].Tip, repo.Head.Tip);
                Assert.Equal(repo.Head.Tip, mergeResult.Commit);

                Assert.Empty(repo.RetrieveStatus());
                Assert.Equal(shouldMergeOccurInDetachedHeadState, repo.Info.IsHeadDetached);

                if (!shouldMergeOccurInDetachedHeadState)
                {
                    // Ensure HEAD is still attached and points to SecondBranch
                    Assert.Equal(repo.Refs.Head.TargetIdentifier, secondBranch.CanonicalName);
                }
            }
        }

        [Fact]
        public void ConflictingMergeRepos()
        {
            const string firstBranchFileName = "first branch file.txt";
            const string secondBranchFileName = "second branch file.txt";
            const string sharedBranchFileName = "first+second branch file.txt";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                Commands.Checkout(repo, firstBranch);

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");
                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "The first branches comment");  // Change file in first branch

                Commands.Checkout(repo, secondBranch);
                // Commit with ONE new file to second branch (FirstBranch and SecondBranch now point to separate commits that both have the same parent commit).
                AddFileCommitToRepo(repo, secondBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "The second branches comment");  // Change file in second branch

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.Conflicts, mergeResult.Status);

                Assert.Null(mergeResult.Commit);
                Assert.Single(repo.Index.Conflicts);

                var conflict = repo.Index.Conflicts.First();
                var changes = repo.Diff.Compare(repo.Lookup<Blob>(conflict.Theirs.Id), repo.Lookup<Blob>(conflict.Ours.Id));

                Assert.False(changes.IsBinaryComparison);
            }
        }

        [Fact]
        public void ConflictingMergeReposBinary()
        {
            const string firstBranchFileName = "first branch file.bin";
            const string secondBranchFileName = "second branch file.bin";
            const string sharedBranchFileName = "first+second branch file.bin";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                Commands.Checkout(repo, firstBranch);

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");
                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "\0The first branches comment\0");  // Change file in first branch

                Commands.Checkout(repo, secondBranch);
                // Commit with ONE new file to second branch (FirstBranch and SecondBranch now point to separate commits that both have the same parent commit).
                AddFileCommitToRepo(repo, secondBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "\0The second branches comment\0");  // Change file in second branch

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.Conflicts, mergeResult.Status);

                Assert.Single(repo.Index.Conflicts);

                Conflict conflict = repo.Index.Conflicts.First();

                var changes = repo.Diff.Compare(repo.Lookup<Blob>(conflict.Theirs.Id), repo.Lookup<Blob>(conflict.Ours.Id));

                Assert.True(changes.IsBinaryComparison);
            }
        }

        [Fact]
        public void CanFailOnFirstMergeConflict()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var mergeResult = repo.Merge("conflicts", Constants.Signature, new MergeOptions() { FailOnConflict = true, });
                Assert.Equal(MergeStatus.Conflicts, mergeResult.Status);

                var master = repo.Branches["master"];
                var branch = repo.Branches["conflicts"];
                var mergeTreeResult = repo.ObjectDatabase.MergeCommits(master.Tip, branch.Tip, new MergeTreeOptions() { FailOnConflict = true });
                Assert.Equal(MergeTreeStatus.Conflicts, mergeTreeResult.Status);
                Assert.Empty(mergeTreeResult.Conflicts);
            }

        }

        [Theory]
        [InlineData(true, FastForwardStrategy.Default, fastForwardBranchInitialId, MergeStatus.FastForward)]
        [InlineData(true, FastForwardStrategy.FastForwardOnly, fastForwardBranchInitialId, MergeStatus.FastForward)]
        [InlineData(false, FastForwardStrategy.Default, fastForwardBranchInitialId, MergeStatus.FastForward)]
        [InlineData(false, FastForwardStrategy.FastForwardOnly, fastForwardBranchInitialId, MergeStatus.FastForward)]
        public void CanFastForwardCommit(bool fromDetachedHead, FastForwardStrategy fastForwardStrategy, string expectedCommitId, MergeStatus expectedMergeStatus)
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                if (fromDetachedHead)
                {
                    Commands.Checkout(repo, repo.Head.Tip.Id.Sha);
                }

                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = fastForwardStrategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.Equal(expectedCommitId, result.Commit.Id.Sha);
                Assert.False(repo.RetrieveStatus().Any());
                Assert.Equal(fromDetachedHead, repo.Info.IsHeadDetached);
            }
        }

        [Theory]
        [InlineData(true, FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData(true, FastForwardStrategy.NoFastForward, MergeStatus.NonFastForward)]
        [InlineData(false, FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData(false, FastForwardStrategy.NoFastForward, MergeStatus.NonFastForward)]
        public void CanNonFastForwardMergeCommit(bool fromDetachedHead, FastForwardStrategy fastForwardStrategy, MergeStatus expectedMergeStatus)
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                if (fromDetachedHead)
                {
                    Commands.Checkout(repo, repo.Head.Tip.Id.Sha);
                }

                Commit commitToMerge = repo.Branches["normal_merge"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = fastForwardStrategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.False(repo.RetrieveStatus().Any());
                Assert.Equal(fromDetachedHead, repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void MergeReportsCheckoutProgress()
        {
            string repoPath = SandboxMergeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Commit commitToMerge = repo.Branches["normal_merge"].Tip;

                bool wasCalled = false;

                MergeOptions options = new MergeOptions()
                {
                    OnCheckoutProgress = (path, completed, total) => wasCalled = true,
                };

                repo.Merge(commitToMerge, Constants.Signature, options);

                Assert.True(wasCalled);
            }
        }

        [Fact]
        public void MergeReportsCheckoutNotifications()
        {
            string repoPath = SandboxMergeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Commit commitToMerge = repo.Branches["normal_merge"].Tip;

                bool wasCalled = false;
                CheckoutNotifyFlags actualNotifyFlags = CheckoutNotifyFlags.None;

                MergeOptions options = new MergeOptions()
                {
                    OnCheckoutNotify = (path, notificationType) => { wasCalled = true; actualNotifyFlags = notificationType; return true; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
                };

                repo.Merge(commitToMerge, Constants.Signature, options);

                Assert.True(wasCalled);
                Assert.Equal(CheckoutNotifyFlags.Updated, actualNotifyFlags);
            }
        }

        [Fact]
        public void FastForwardMergeReportsCheckoutProgress()
        {
            string repoPath = SandboxMergeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                bool wasCalled = false;

                MergeOptions options = new MergeOptions()
                {
                    OnCheckoutProgress = (path, completed, total) => wasCalled = true,
                };

                repo.Merge(commitToMerge, Constants.Signature, options);

                Assert.True(wasCalled);
            }
        }

        [Fact]
        public void FastForwardMergeReportsCheckoutNotifications()
        {
            string repoPath = SandboxMergeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                bool wasCalled = false;
                CheckoutNotifyFlags actualNotifyFlags = CheckoutNotifyFlags.None;

                MergeOptions options = new MergeOptions()
                {
                    OnCheckoutNotify = (path, notificationType) => { wasCalled = true; actualNotifyFlags = notificationType; return true; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
                };

                repo.Merge(commitToMerge, Constants.Signature, options);

                Assert.True(wasCalled);
                Assert.Equal(CheckoutNotifyFlags.Updated, actualNotifyFlags);
            }
        }

        [Fact]
        public void MergeCanDetectRenames()
        {
            // The environment is set up such that:
            // file b.txt is edited in the "rename" branch and
            // edited and renamed in the "rename_source" branch.
            // The edits are automergable.
            // We can rename "rename_source" into "rename"
            // if rename detection is enabled,
            // but the merge will fail with conflicts if this
            // change is not detected as a rename.

            string repoPath = SandboxMergeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Branch currentBranch = Commands.Checkout(repo, "rename_source");
                Assert.NotNull(currentBranch);

                Branch branchToMerge = repo.Branches["rename"];

                MergeResult result = repo.Merge(branchToMerge, Constants.Signature);

                Assert.Equal(MergeStatus.NonFastForward, result.Status);
            }
        }

        [Fact]
        public void FastForwardNonFastForwardableMergeThrows()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["normal_merge"].Tip;
                Assert.Throws<NonFastForwardException>(() => repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = FastForwardStrategy.FastForwardOnly }));
            }
        }

        [Fact]
        public void CanForceFastForwardMergeThroughConfig()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("merge.ff", "only");

                Commit commitToMerge = repo.Branches["normal_merge"].Tip;
                Assert.Throws<NonFastForwardException>(() => repo.Merge(commitToMerge, Constants.Signature, new MergeOptions()));
            }
        }

        [Fact]
        public void CanMergeAndNotCommit()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["normal_merge"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { CommitOnSuccess = false });

                Assert.Equal(MergeStatus.NonFastForward, result.Status);
                Assert.Null(result.Commit);

                RepositoryStatus repoStatus = repo.RetrieveStatus();

                // Verify that there is a staged entry.
                Assert.Single(repoStatus);
                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus("b.txt"));
            }
        }

        [Fact]
        public void CanForceNonFastForwardMerge()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = FastForwardStrategy.NoFastForward });

                Assert.Equal(MergeStatus.NonFastForward, result.Status);
                Assert.Equal("f58f780d5a0ae392efd4a924450b1bbdc0577d32", result.Commit.Id.Sha);
                Assert.False(repo.RetrieveStatus().Any());
            }
        }

        [Fact]
        public void CanForceNonFastForwardMergeThroughConfig()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("merge.ff", "false");

                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions());

                Assert.Equal(MergeStatus.NonFastForward, result.Status);
                Assert.Equal("f58f780d5a0ae392efd4a924450b1bbdc0577d32", result.Commit.Id.Sha);
                Assert.False(repo.RetrieveStatus().Any());
            }
        }

        [Fact]
        public void VerifyUpToDateMerge()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["master"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = FastForwardStrategy.NoFastForward });

                Assert.Equal(MergeStatus.UpToDate, result.Status);
                Assert.Null(result.Commit);
                Assert.False(repo.RetrieveStatus().Any());
            }
        }

        [Theory]
        [InlineData("refs/heads/normal_merge", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("normal_merge", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("625186280ed2a6ec9b65d250ed90cf2e4acef957", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("fast_forward", FastForwardStrategy.Default, MergeStatus.FastForward)]
        public void CanMergeCommittish(string committish, FastForwardStrategy strategy, MergeStatus expectedMergeStatus)
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                MergeResult result = repo.Merge(committish, Constants.Signature, new MergeOptions() { FastForwardStrategy = strategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.False(repo.RetrieveStatus().Any());
            }
        }

        [Theory]
        [InlineData(true, FastForwardStrategy.FastForwardOnly)]
        [InlineData(false, FastForwardStrategy.FastForwardOnly)]
        [InlineData(true, FastForwardStrategy.NoFastForward)]
        [InlineData(false, FastForwardStrategy.NoFastForward)]
        public void MergeWithWorkDirConflictsThrows(bool shouldStage, FastForwardStrategy strategy)
        {
            // Merging the fast_forward branch results in a change to file
            // b.txt. In this test we modify the file in the working directory
            // and then attempt to perform a merge. We expect the merge to fail
            // due to checkout conflicts.
            string committishToMerge = "fast_forward";

            using (var repo = new Repository(SandboxMergeTestRepo()))
            {
                Touch(repo.Info.WorkingDirectory, "b.txt", "this is an alternate change");

                if (shouldStage)
                {
                    Commands.Stage(repo, "b.txt");
                }

                Assert.Throws<CheckoutConflictException>(() => repo.Merge(committishToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = strategy }));
            }
        }

        [Theory]
        [InlineData(CheckoutFileConflictStrategy.Ours)]
        [InlineData(CheckoutFileConflictStrategy.Theirs)]
        public void CanSpecifyConflictFileStrategy(CheckoutFileConflictStrategy conflictStrategy)
        {
            const string conflictFile = "a.txt";
            const string conflictBranchName = "conflicts";

            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.Branches[conflictBranchName];
                Assert.NotNull(branch);

                MergeOptions mergeOptions = new MergeOptions()
                {
                    FileConflictStrategy = conflictStrategy,
                };

                MergeResult result = repo.Merge(branch, Constants.Signature, mergeOptions);
                Assert.Equal(MergeStatus.Conflicts, result.Status);

                // Get the information on the conflict.
                Conflict conflict = repo.Index.Conflicts[conflictFile];

                Assert.NotNull(conflict);
                Assert.NotNull(conflict.Theirs);
                Assert.NotNull(conflict.Ours);

                // Get the blob containing the expected content.
                Blob expectedBlob = null;
                switch (conflictStrategy)
                {
                    case CheckoutFileConflictStrategy.Theirs:
                        expectedBlob = repo.Lookup<Blob>(conflict.Theirs.Id);
                        break;
                    case CheckoutFileConflictStrategy.Ours:
                        expectedBlob = repo.Lookup<Blob>(conflict.Ours.Id);
                        break;
                    default:
                        throw new Exception("Unexpected FileConflictStrategy");
                }

                Assert.NotNull(expectedBlob);

                // Check the content of the file on disk matches what is expected.
                string expectedContent = expectedBlob.GetContentText(new FilteringOptions(conflictFile));
                Assert.Equal(expectedContent, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, conflictFile)));
            }
        }

        [Theory]
        [InlineData(MergeFileFavor.Ours)]
        [InlineData(MergeFileFavor.Theirs)]
        public void MergeCanSpecifyMergeFileFavorOption(MergeFileFavor fileFavorFlag)
        {
            const string conflictFile = "a.txt";
            const string conflictBranchName = "conflicts";

            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.Branches[conflictBranchName];
                Assert.NotNull(branch);

                MergeOptions mergeOptions = new MergeOptions()
                {
                    MergeFileFavor = fileFavorFlag,
                };

                MergeResult result = repo.Merge(branch, Constants.Signature, mergeOptions);

                Assert.Equal(MergeStatus.NonFastForward, result.Status);

                // Verify that the index and working directory are clean
                Assert.True(repo.Index.IsFullyMerged);
                Assert.False(repo.RetrieveStatus().IsDirty);

                // Get the blob containing the expected content.
                Blob expectedBlob = null;
                switch (fileFavorFlag)
                {
                    case MergeFileFavor.Theirs:
                        expectedBlob = repo.Lookup<Blob>("3dd9738af654bbf1c363f6c3bbc323bacdefa179");
                        break;
                    case MergeFileFavor.Ours:
                        expectedBlob = repo.Lookup<Blob>("610b16886ca829cebd2767d9196f3c4378fe60b5");
                        break;
                    default:
                        throw new Exception("Unexpected MergeFileFavor");
                }

                Assert.NotNull(expectedBlob);

                // Verify the index has the expected contents
                IndexEntry entry = repo.Index[conflictFile];
                Assert.NotNull(entry);
                Assert.Equal(expectedBlob.Id, entry.Id);

                // Verify the content of the file on disk matches what is expected.
                string expectedContent = expectedBlob.GetContentText(new FilteringOptions(conflictFile));
                Assert.Equal(expectedContent, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, conflictFile)));
            }
        }

        [Theory]
        [InlineData("refs/heads/normal_merge", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("fast_forward", FastForwardStrategy.Default, MergeStatus.FastForward)]
        public void CanMergeBranch(string branchName, FastForwardStrategy strategy, MergeStatus expectedMergeStatus)
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.Branches[branchName];
                MergeResult result = repo.Merge(branch, Constants.Signature, new MergeOptions() { FastForwardStrategy = strategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.False(repo.RetrieveStatus().Any());
            }
        }

        [Fact]
        public void CanMergeIntoOrphanedBranch()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.Add("HEAD", "refs/heads/orphan", true);

                // Remove entries from the working directory
                foreach (var entry in repo.RetrieveStatus())
                {
                    Commands.Unstage(repo, entry.FilePath);
                    Commands.Remove(repo, entry.FilePath, true);
                }

                // Assert that we have an empty working directory.
                Assert.False(repo.RetrieveStatus().Any());

                MergeResult result = repo.Merge("master", Constants.Signature);

                Assert.Equal(MergeStatus.FastForward, result.Status);
                Assert.Equal(masterBranchInitialId, result.Commit.Id.Sha);
                Assert.False(repo.RetrieveStatus().Any());
            }
        }


        [Fact]
        public void CanMergeTreeIntoSameTree()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Branches["master"].Tip;

                var result = repo.ObjectDatabase.MergeCommits(master, master, null);
                Assert.Equal(MergeTreeStatus.Succeeded, result.Status);
                Assert.Empty(result.Conflicts);
            }
        }

        [Fact]
        public void CanMergeTreeIntoTreeFromUnbornBranch()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/unborn");

                Touch(repo.Info.WorkingDirectory, "README", "Yeah!\n");
                repo.Index.Clear();
                Commands.Stage(repo, "README");

                repo.Commit("A new world, free of the burden of the history", Constants.Signature, Constants.Signature);

                var master = repo.Branches["master"].Tip;
                var branch = repo.Branches["unborn"].Tip;

                var result = repo.ObjectDatabase.MergeCommits(master, branch, null);
                Assert.Equal(MergeTreeStatus.Succeeded, result.Status);
                Assert.NotNull(result.Tree);
                Assert.Empty(result.Conflicts);
            }
        }

        [Fact]
        public void CanMergeCommitsAndDetectConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/unborn");

                repo.Index.Replace(repo.Lookup<Commit>("conflicts"));

                repo.Commit("A conflicting world, free of the burden of the history", Constants.Signature, Constants.Signature);

                var master = repo.Branches["master"].Tip;
                var branch = repo.Branches["unborn"].Tip;

                var result = repo.ObjectDatabase.MergeCommits(master, branch, null);
                Assert.Equal(MergeTreeStatus.Conflicts, result.Status);
                Assert.Null(result.Tree);
                Assert.NotEmpty(result.Conflicts);
            }
        }

        [Fact]
        public void CanMergeFastForwardTreeWithoutConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Lookup<Commit>("master");
                var branch = repo.Lookup<Commit>("fast_forward");

                var result = repo.ObjectDatabase.MergeCommits(master, branch, null);
                Assert.Equal(MergeTreeStatus.Succeeded, result.Status);
                Assert.NotNull(result.Tree);
                Assert.Empty(result.Conflicts);
            }
        }

        [Fact]
        public void CanIdentifyConflictsInMergeCommits()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Lookup<Commit>("master");
                var branch = repo.Lookup<Commit>("conflicts");

                var result = repo.ObjectDatabase.MergeCommits(master, branch, null);

                Assert.Equal(MergeTreeStatus.Conflicts, result.Status);

                Assert.Null(result.Tree);
                Assert.Single(result.Conflicts);

                var conflict = result.Conflicts.First();
                Assert.Equal(new ObjectId("8e9daea300fbfef6c0da9744c6214f546d55b279"), conflict.Ancestor.Id);
                Assert.Equal(new ObjectId("610b16886ca829cebd2767d9196f3c4378fe60b5"), conflict.Ours.Id);
                Assert.Equal(new ObjectId("3dd9738af654bbf1c363f6c3bbc323bacdefa179"), conflict.Theirs.Id);
            }
        }

        [Theory]
        [InlineData("conflicts_spaces")]
        [InlineData("conflicts_tabs")]
        public void CanConflictOnWhitespaceChangeMergeConflict(string branchName)
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var mergeResult = repo.Merge(branchName, Constants.Signature, new MergeOptions());
                Assert.Equal(MergeStatus.Conflicts, mergeResult.Status);

                var master = repo.Branches["master"];
                var branch = repo.Branches[branchName];
                var mergeTreeResult = repo.ObjectDatabase.MergeCommits(master.Tip, branch.Tip, new MergeTreeOptions());
                Assert.Equal(MergeTreeStatus.Conflicts, mergeTreeResult.Status);
            }
        }

        [Theory]
        [InlineData("conflicts_spaces")]
        [InlineData("conflicts_tabs")]
        public void CanIgnoreWhitespaceChangeMergeConflict(string branchName)
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var mergeResult = repo.Merge(branchName, Constants.Signature, new MergeOptions() { IgnoreWhitespaceChange = true });
                Assert.NotEqual(MergeStatus.Conflicts, mergeResult.Status);

                var master = repo.Branches["master"];
                var branch = repo.Branches[branchName];
                var mergeTreeResult = repo.ObjectDatabase.MergeCommits(master.Tip, branch.Tip, new MergeTreeOptions() { IgnoreWhitespaceChange = true });
                Assert.NotEqual(MergeTreeStatus.Conflicts, mergeTreeResult.Status);
                Assert.Empty(mergeTreeResult.Conflicts);
            }
        }

        [Fact]
        public void CanMergeIntoIndex()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Lookup<Commit>("master");

                using (TransientIndex index = repo.ObjectDatabase.MergeCommitsIntoIndex(master, master, null))
                {
                    var tree = index.WriteToTree();
                    Assert.Equal(master.Tree.Id, tree.Id);
                }
            }
        }

        [Fact]
        public void CanMergeIntoIndexWithConflicts()
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Lookup<Commit>("master");
                var branch = repo.Lookup<Commit>("conflicts");

                using (TransientIndex index = repo.ObjectDatabase.MergeCommitsIntoIndex(branch, master, null))
                {
                    Assert.False(index.IsFullyMerged);

                    var conflict = index.Conflicts.First();

                    //Resolve the conflict by taking the blob from branch
                    var blob = repo.Lookup<Blob>(conflict.Ours.Id);
                    //Add() does not remove conflict entries for the same path, so they must be explicitly removed first.
                    index.Remove(conflict.Ours.Path);
                    index.Add(blob, conflict.Ours.Path, Mode.NonExecutableFile);

                    Assert.True(index.IsFullyMerged);
                    var tree = index.WriteToTree();

                    //Since we took the conflicted blob from the branch, the merged result should be the same as the branch.
                    Assert.Equal(branch.Tree.Id, tree.Id);
                }
            }
        }

        private Commit AddFileCommitToRepo(IRepository repository, string filename, string content = null)
        {
            Touch(repository.Info.WorkingDirectory, filename, content);

            Commands.Stage(repository, filename);

            return repository.Commit("New commit", Constants.Signature, Constants.Signature);
        }

        // Commit IDs of the checked in merge_testrepo
        private const string masterBranchInitialId = "83cebf5389a4adbcb80bda6b68513caee4559802";
        private const string fastForwardBranchInitialId = "4dfaa1500526214ae7b33f9b2c1144ca8b6b1f53";
    }
}
