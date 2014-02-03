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
                Assert.Empty(repo.MergeHeads);
            }
        }

        [Fact]
        public void AFullyMergedRepoOnlyContainsStagedIndexEntries()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Equal(true, repo.Index.IsFullyMerged);
                Assert.Empty(repo.MergeHeads);

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

                MergeHead[] mergedHeads = repo.MergeHeads.ToArray();
                Assert.Equal("MERGE_HEAD[0]", mergedHeads[0].Name);
                Assert.Equal(firstBranch, mergedHeads[0].Tip.Id.Sha);
                Assert.Equal("MERGE_HEAD[1]", mergedHeads[1].Name);
                Assert.Null(mergedHeads[1].Tip);
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

        private Commit AddFileCommitToRepo(IRepository repository, string filename, string content = null)
        {
            Touch(repository.Info.WorkingDirectory, filename, content);

            repository.Index.Stage(filename);

            return repository.Commit("New commit", Constants.Signature, Constants.Signature);
        }
    }
}
