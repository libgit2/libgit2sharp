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
                Assert.Equal(true, repo.Index.IsFullyMerged);
            }
        }

        [Fact]
        public void AFullyMergedRepoOnlyContainsStagedIndexEntries()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Equal(true, repo.Index.IsFullyMerged);

                foreach (var entry in repo.Index)
                {
                    Assert.Equal(StageLevel.Staged, entry.StageLevel);
                }
            }
        }

        [Fact]
        public void SoftResetARepoWithUnmergedEntriesThrows()
        {
            using (var repo = new Repository(MergedTestRepoWorkingDirPath))
            {
                Assert.Equal(false, repo.Index.IsFullyMerged);

                var headCommit = repo.Head.Tip;
                var firstCommitParent = headCommit.Parents.First();
                Assert.Throws<UnmergedIndexEntriesException>(
                    () => repo.Reset(ResetMode.Soft, firstCommitParent));
            }
        }

        [Fact]
        public void CommitAgainARepoWithUnmergedEntriesThrows()
        {
            using (var repo = new Repository(MergedTestRepoWorkingDirPath))
            {
                Assert.Equal(false, repo.Index.IsFullyMerged);

                var author = Constants.Signature;
                Assert.Throws<UnmergedIndexEntriesException>(
                    () => repo.Commit("Try commit unmerged entries", author, author));
            }
        }

        [Fact]
        public void CanRetrieveTheBranchBeingMerged()
        {
            string path = CloneStandardTestRepo();
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

            string path = CloneStandardTestRepo();

            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                firstBranch.Checkout();
                var originalTreeCount = firstBranch.Tip.Tree.Count;

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");
                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);

                if (shouldMergeOccurInDetachedHeadState)
                {
                    // Detaches HEAD
                    repo.Checkout(secondBranch.Tip);
                }
                else
                {
                    secondBranch.Checkout();
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

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                firstBranch.Checkout();

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");

                secondBranch.Checkout();

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

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Reset the index and the working tree.
                repo.Reset(ResetMode.Hard);

                // Clean the working directory.
                repo.RemoveUntrackedFiles();

                var firstBranch = repo.CreateBranch("FirstBranch");
                firstBranch.Checkout();

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");

                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);

                if (shouldMergeOccurInDetachedHeadState)
                {
                    // Detaches HEAD
                    repo.Checkout(secondBranch.Tip);
                }
                else
                {
                    secondBranch.Checkout();
                }

                Assert.Equal(shouldMergeOccurInDetachedHeadState, repo.Info.IsHeadDetached);

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.FastForward, mergeResult.Status);
                Assert.Equal(repo.Branches["FirstBranch"].Tip, mergeResult.Commit);
                Assert.Equal(repo.Branches["FirstBranch"].Tip, repo.Head.Tip);
                Assert.Equal(repo.Head.Tip, mergeResult.Commit);

                Assert.Equal(0, repo.Index.RetrieveStatus().Count());
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

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                firstBranch.Checkout();

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");
                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "The first branches comment");  // Change file in first branch

                secondBranch.Checkout();
                // Commit with ONE new file to second branch (FirstBranch and SecondBranch now point to separate commits that both have the same parent commit).
                AddFileCommitToRepo(repo, secondBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "The second branches comment");  // Change file in second branch

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.Conflicts, mergeResult.Status);

                Assert.Null(mergeResult.Commit);
                Assert.Equal(1, repo.Index.Conflicts.Count());

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

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                firstBranch.Checkout();

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");
                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "\0The first branches comment\0");  // Change file in first branch

                secondBranch.Checkout();
                // Commit with ONE new file to second branch (FirstBranch and SecondBranch now point to separate commits that both have the same parent commit).
                AddFileCommitToRepo(repo, secondBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "\0The second branches comment\0");  // Change file in second branch

                MergeResult mergeResult = repo.Merge(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(MergeStatus.Conflicts, mergeResult.Status);

                Assert.Equal(1, repo.Index.Conflicts.Count());

                Conflict conflict = repo.Index.Conflicts.First();

                var changes = repo.Diff.Compare(repo.Lookup<Blob>(conflict.Theirs.Id), repo.Lookup<Blob>(conflict.Ours.Id));

                Assert.True(changes.IsBinaryComparison);
            }
        }

        [Theory]
        [InlineData(true, FastForwardStrategy.Default, fastForwardBranchInitialId, MergeStatus.FastForward)]
        [InlineData(true, FastForwardStrategy.FastForwardOnly, fastForwardBranchInitialId, MergeStatus.FastForward)]
        [InlineData(false, FastForwardStrategy.Default, fastForwardBranchInitialId, MergeStatus.FastForward)]
        [InlineData(false, FastForwardStrategy.FastForwardOnly, fastForwardBranchInitialId, MergeStatus.FastForward)]
        public void CanFastForwardCommit(bool fromDetachedHead, FastForwardStrategy fastForwardStrategy, string expectedCommitId, MergeStatus expectedMergeStatus)
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                if(fromDetachedHead)
                {
                    repo.Checkout(repo.Head.Tip.Id.Sha);
                }

                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = fastForwardStrategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.Equal(expectedCommitId, result.Commit.Id.Sha);
                Assert.False(repo.Index.RetrieveStatus().Any());
                Assert.Equal(fromDetachedHead, repo.Info.IsHeadDetached);
            }
        }

        [Theory]
        [InlineData(true, FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData(true, FastForwardStrategy.NoFastFoward, MergeStatus.NonFastForward)]
        [InlineData(false, FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData(false, FastForwardStrategy.NoFastFoward, MergeStatus.NonFastForward)]
        public void CanNonFastForwardMergeCommit(bool fromDetachedHead, FastForwardStrategy fastForwardStrategy, MergeStatus expectedMergeStatus)
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                if (fromDetachedHead)
                {
                    repo.Checkout(repo.Head.Tip.Id.Sha);
                }

                Commit commitToMerge = repo.Branches["normal_merge"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = fastForwardStrategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.False(repo.Index.RetrieveStatus().Any());
                Assert.Equal(fromDetachedHead, repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void FastForwardNonFastForwardableMergeThrows()
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["normal_merge"].Tip;
                Assert.Throws<NonFastForwardException>(() => repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = FastForwardStrategy.FastForwardOnly }));
            }
        }

        [Fact]
        public void CanMergeAndNotCommit()
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["normal_merge"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { CommitOnSuccess = false});

                Assert.Equal(MergeStatus.NonFastForward, result.Status);
                Assert.Equal(null, result.Commit);

                RepositoryStatus repoStatus = repo.Index.RetrieveStatus();

                // Verify that there is a staged entry.
                Assert.Equal(1, repoStatus.Count());
                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus("b.txt"));
            }
        }

        [Fact]
        public void CanForceNonFastForwardMerge()
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = FastForwardStrategy.NoFastFoward });

                Assert.Equal(MergeStatus.NonFastForward, result.Status);
                Assert.Equal("f58f780d5a0ae392efd4a924450b1bbdc0577d32", result.Commit.Id.Sha);
                Assert.False(repo.Index.RetrieveStatus().Any());
            }
        }

        [Fact]
        public void VerifyUpToDateMerge()
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commitToMerge = repo.Branches["master"].Tip;

                MergeResult result = repo.Merge(commitToMerge, Constants.Signature, new MergeOptions() { FastForwardStrategy = FastForwardStrategy.NoFastFoward });

                Assert.Equal(MergeStatus.UpToDate, result.Status);
                Assert.Equal(null, result.Commit);
                Assert.False(repo.Index.RetrieveStatus().Any());
            }
        }

        [Theory]
        [InlineData("refs/heads/normal_merge", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("normal_merge", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("625186280ed2a6ec9b65d250ed90cf2e4acef957", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("fast_forward", FastForwardStrategy.Default, MergeStatus.FastForward)]
        public void CanMergeCommittish(string committish, FastForwardStrategy strategy, MergeStatus expectedMergeStatus)
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                MergeResult result = repo.Merge(committish, Constants.Signature, new MergeOptions() { FastForwardStrategy = strategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.False(repo.Index.RetrieveStatus().Any());
            }
        }

        [Theory]
        [InlineData("refs/heads/normal_merge", FastForwardStrategy.Default, MergeStatus.NonFastForward)]
        [InlineData("fast_forward", FastForwardStrategy.Default, MergeStatus.FastForward)]
        public void CanMergeBranch(string branchName, FastForwardStrategy strategy, MergeStatus expectedMergeStatus)
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo. Branches[branchName];
                MergeResult result = repo.Merge(branch, Constants.Signature, new MergeOptions() { FastForwardStrategy = strategy });

                Assert.Equal(expectedMergeStatus, result.Status);
                Assert.False(repo.Index.RetrieveStatus().Any());
            }
        }

        [Fact]
        public void CanMergeIntoOrphanedBranch()
        {
            string path = CloneMergeTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.Add("HEAD", "refs/heads/orphan", true);

                // Remove entries from the working directory
                foreach(var entry in repo.Index.RetrieveStatus())
                {
                    repo.Index.Unstage(entry.FilePath);
                    repo.Index.Remove(entry.FilePath, true);
                }

                // Assert that we have an empty working directory.
                Assert.False(repo.Index.RetrieveStatus().Any());

                MergeResult result = repo.Merge("master", Constants.Signature);

                Assert.Equal(MergeStatus.FastForward, result.Status);
                Assert.Equal(masterBranchInitialId, result.Commit.Id.Sha);
                Assert.False(repo.Index.RetrieveStatus().Any());
            }
        }

        private Commit AddFileCommitToRepo(IRepository repository, string filename, string content = null)
        {
            Touch(repository.Info.WorkingDirectory, filename, content);

            repository.Index.Stage(filename);

            return repository.Commit("New commit", Constants.Signature, Constants.Signature);
        }

        // Commit IDs of the checked in merge_testrepo
        private const string masterBranchInitialId = "83cebf5389a4adbcb80bda6b68513caee4559802";
        private const string fastForwardBranchInitialId = "4dfaa1500526214ae7b33f9b2c1144ca8b6b1f53";
    }
}
