using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RevertFixture : BaseFixture
    {
        [Fact]
        public void CanRevert()
        {
            // The branch name to perform the revert on,
            // and the file whose contents we expect to be reverted.
            const string revertBranchName = "refs/heads/revert";
            const string revertedFile = "a.txt";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                // Revert tip commit.
                RevertResult result = repo.Revert(repo.Head.Tip, Constants.Signature);
                Assert.NotNull(result);
                Assert.Equal(RevertStatus.Reverted, result.Status);

                // Verify commit was made.
                Assert.NotNull(result.Commit);

                // Verify the expected commit ID.
                Assert.Equal("04746060fa753c9970d88a0b59151d7b212ac903", result.Commit.Id.Sha);

                // Verify workspace is clean.
                Assert.True(repo.Index.IsFullyMerged);
                Assert.False(repo.RetrieveStatus().IsDirty);

                // Lookup the blob containing the expected reverted content of a.txt.
                Blob expectedBlob = repo.Lookup<Blob>("bc90ea420cf6c5ae3db7dcdffa0d79df567f219b");
                Assert.NotNull(expectedBlob);

                // Verify contents of Index.
                IndexEntry revertedIndexEntry = repo.Index[revertedFile];
                Assert.NotNull(revertedIndexEntry);

                // Verify the contents of the index.
                Assert.Equal(expectedBlob.Id, revertedIndexEntry.Id);

                // Verify contents of workspace.
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, revertedFile);
                Assert.Equal(expectedBlob.GetContentText(new FilteringOptions(revertedFile)), File.ReadAllText(fullPath));
            }
        }

        [Fact]
        public void CanRevertAndNotCommit()
        {
            // The branch name to perform the revert on,
            // and the file whose contents we expect to be reverted.
            const string revertBranchName = "refs/heads/revert";
            const string revertedFile = "a.txt";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                // Revert tip commit.
                RevertResult result = repo.Revert(repo.Head.Tip, Constants.Signature, new RevertOptions() { CommitOnSuccess = false });
                Assert.NotNull(result);
                Assert.Equal(RevertStatus.Reverted, result.Status);

                // Verify the commit was made.
                Assert.Null(result.Commit);

                // Verify workspace is dirty.
                FileStatus fileStatus = repo.RetrieveStatus(revertedFile);
                Assert.Equal(FileStatus.ModifiedInIndex, fileStatus);

                // This is the ID of the blob containing the expected content.
                Blob expectedBlob = repo.Lookup<Blob>("bc90ea420cf6c5ae3db7dcdffa0d79df567f219b");
                Assert.NotNull(expectedBlob);

                // Verify contents of Index.
                IndexEntry revertedIndexEntry = repo.Index[revertedFile];
                Assert.NotNull(revertedIndexEntry);

                Assert.Equal(expectedBlob.Id, revertedIndexEntry.Id);

                // Verify contents of workspace.
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, revertedFile);
                Assert.Equal(expectedBlob.GetContentText(new FilteringOptions(revertedFile)), File.ReadAllText(fullPath));
            }
        }

        [Fact]
        public void RevertWithConflictDoesNotCommit()
        {
            // The branch name to perform the revert on,
            // and the file whose contents we expect to be reverted.
            const string revertBranchName = "refs/heads/revert";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                // The commit to revert - we know that reverting this
                // specific commit will generate conflicts.
                Commit commitToRevert = repo.Lookup<Commit>("cb4f7f0eca7a0114cdafd8537332aa17de36a4e9");
                Assert.NotNull(commitToRevert);

                // Perform the revert and verify there were conflicts.
                RevertResult result = repo.Revert(commitToRevert, Constants.Signature);
                Assert.NotNull(result);
                Assert.Equal(RevertStatus.Conflicts, result.Status);
                Assert.Null(result.Commit);

                // Verify there is a conflict on the expected path.
                Assert.False(repo.Index.IsFullyMerged);
                Assert.NotNull(repo.Index.Conflicts["a.txt"]);

                // Verify the non-conflicting paths are staged.
                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus("b.txt"));
                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus("c.txt"));
            }
        }

        [Theory]
        [InlineData(CheckoutFileConflictStrategy.Ours)]
        [InlineData(CheckoutFileConflictStrategy.Theirs)]
        public void RevertWithFileConflictStrategyOption(CheckoutFileConflictStrategy conflictStrategy)
        {
            // The branch name to perform the revert on,
            // and the file which we expect conflicts as result of the revert.
            const string revertBranchName = "refs/heads/revert";
            const string conflictedFilePath = "a.txt";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                // Specify FileConflictStrategy.
                RevertOptions options = new RevertOptions()
                {
                    FileConflictStrategy = conflictStrategy,
                };

                RevertResult result = repo.Revert(repo.Head.Tip.Parents.First(), Constants.Signature, options);
                Assert.Equal(RevertStatus.Conflicts, result.Status);

                // Verify there is a conflict.
                Assert.False(repo.Index.IsFullyMerged);

                Conflict conflict = repo.Index.Conflicts[conflictedFilePath];
                Assert.NotNull(conflict);

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
                string expectedContent = expectedBlob.GetContentText(new FilteringOptions(conflictedFilePath));
                Assert.Equal(expectedContent, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, conflictedFilePath)));
            }
        }

        [Fact]
        public void RevertReportsCheckoutProgress()
        {
            const string revertBranchName = "refs/heads/revert";

            string repoPath = SandboxRevertTestRepo();
            using (var repo = new Repository(repoPath))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                bool wasCalled = false;

                RevertOptions options = new RevertOptions()
                {
                    OnCheckoutProgress = (path, completed, total) => wasCalled = true
                };

                repo.Revert(repo.Head.Tip, Constants.Signature, options);

                Assert.True(wasCalled);
            }
        }

        [Fact]
        public void RevertReportsCheckoutNotification()
        {
            const string revertBranchName = "refs/heads/revert";

            string repoPath = SandboxRevertTestRepo();
            using (var repo = new Repository(repoPath))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                bool wasCalled = false;
                CheckoutNotifyFlags actualNotifyFlags = CheckoutNotifyFlags.None;

                RevertOptions options = new RevertOptions()
                {
                    OnCheckoutNotify = (path, notificationType) => { wasCalled = true; actualNotifyFlags = notificationType; return true; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
                };

                repo.Revert(repo.Head.Tip, Constants.Signature, options);

                Assert.True(wasCalled);
                Assert.Equal(CheckoutNotifyFlags.Updated, actualNotifyFlags);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        public void RevertFindsRenames(bool? findRenames)
        {
            // The environment is set up such that:
            //   - file d.txt is edited in the commit that is to be reverted (commit A)
            //   - file d.txt is renamed to d_renamed.txt
            //   - commit A is reverted.
            // If rename detection is enabled, then the revert is applied
            // to d_renamed.txt. If rename detection is not enabled,
            // then the revert results in a conflict.
            const string revertBranchName = "refs/heads/revert_rename";
            const string commitIdToRevert = "ca3e813";
            const string expectedBlobId = "0ff3bbb9c8bba2291654cd64067fa417ff54c508";
            const string modifiedFilePath = "d_renamed.txt";

            string repoPath = SandboxRevertTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Branch currentBranch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(currentBranch);

                Commit commitToRevert = repo.Lookup<Commit>(commitIdToRevert);
                Assert.NotNull(currentBranch);

                RevertOptions options;
                if (findRenames.HasValue)
                {
                    options = new RevertOptions()
                    {
                        FindRenames = findRenames.Value,
                    };
                }
                else
                {
                    options = new RevertOptions();
                }

                RevertResult result = repo.Revert(commitToRevert, Constants.Signature, options);
                Assert.NotNull(result);

                if (!findRenames.HasValue ||
                    findRenames.Value == true)
                {
                    Assert.Equal(RevertStatus.Reverted, result.Status);
                    Assert.NotNull(result.Commit);
                    Blob expectedBlob = repo.Lookup<Blob>(expectedBlobId);
                    Assert.NotNull(expectedBlob);

                    GitObject blob = result.Commit.Tree[modifiedFilePath].Target as Blob;
                    Assert.NotNull(blob);
                    Assert.Equal(blob.Id, expectedBlob.Id);

                    // Verify contents of workspace
                    string fullPath = Path.Combine(repo.Info.WorkingDirectory, modifiedFilePath);
                    Assert.Equal(expectedBlob.GetContentText(new FilteringOptions(modifiedFilePath)), File.ReadAllText(fullPath));
                }
                else
                {
                    Assert.Equal(RevertStatus.Conflicts, result.Status);
                    Assert.Null(result.Commit);
                }
            }
        }

        [Theory]
        [InlineData(1, "a04ef5f22c2413a9743046436c0e5354ed903f78")]
        [InlineData(2, "1ae0cd88802bb4f4e6413ba63e41376d235b6fd0")]
        public void CanRevertMergeCommit(int mainline, string expectedId)
        {
            const string revertBranchName = "refs/heads/revert_merge";
            const string commitIdToRevert = "2747045";

            string repoPath = SandboxRevertTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                Commit commitToRevert = repo.Lookup<Commit>(commitIdToRevert);
                Assert.NotNull(commitToRevert);

                RevertOptions options = new RevertOptions()
                {
                    Mainline = mainline,
                };

                RevertResult result = repo.Revert(commitToRevert, Constants.Signature, options);

                Assert.NotNull(result);
                Assert.Equal(RevertStatus.Reverted, result.Status);
                Assert.Equal(result.Commit.Sha, expectedId);

                if (mainline == 1)
                {
                    // In this case, we expect "d_renamed.txt" to be reverted (deleted),
                    // and a.txt to match the tip of the "revert" branch.
                    Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus("d_renamed.txt"));

                    // This is the commit containing the expected contents of a.txt.
                    Commit commit = repo.Lookup<Commit>("b6fbb29b625aabe0fb5736da6fd61d4147e4405e");
                    Assert.NotNull(commit);
                    Assert.Equal(commit["a.txt"].Target.Id, repo.Index["a.txt"].Id);
                }
                else if (mainline == 2)
                {
                    // In this case, we expect "d_renamed.txt" to be preset,
                    // and a.txt to match the tip of the master branch.

                    // In this case, we expect "d_renamed.txt" to be reverted (deleted),
                    // and a.txt to match the tip of the "revert" branch.
                    Assert.Equal(FileStatus.Unaltered, repo.RetrieveStatus("d_renamed.txt"));

                    // This is the commit containing the expected contents of "d_renamed.txt".
                    Commit commit = repo.Lookup<Commit>("c4b5cea70e4cd5b633ed0f10ae0ed5384e8190d8");
                    Assert.NotNull(commit);
                    Assert.Equal(commit["d_renamed.txt"].Target.Id, repo.Index["d_renamed.txt"].Id);

                    // This is the commit containing the expected contents of a.txt.
                    commit = repo.Lookup<Commit>("cb4f7f0eca7a0114cdafd8537332aa17de36a4e9");
                    Assert.NotNull(commit);
                    Assert.Equal(commit["a.txt"].Target.Id, repo.Index["a.txt"].Id);
                }
            }
        }

        [Fact]
        public void CanNotRevertAMergeCommitWithoutSpecifyingTheMainlineBranch()
        {
            const string revertBranchName = "refs/heads/revert_merge";
            const string commitIdToRevert = "2747045";

            string repoPath = SandboxRevertTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                var commitToRevert = repo.Lookup<Commit>(commitIdToRevert);
                Assert.NotNull(commitToRevert);

                Assert.Throws<LibGit2SharpException>(() => repo.Revert(commitToRevert, Constants.Signature));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RevertWithNothingToRevert(bool commitOnSuccess)
        {
            // The branch name to perform the revert on
            const string revertBranchName = "refs/heads/revert";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                Commit commitToRevert = repo.Head.Tip;

                // Revert tip commit.
                RevertResult result = repo.Revert(commitToRevert, Constants.Signature);
                Assert.NotNull(result);
                Assert.Equal(RevertStatus.Reverted, result.Status);

                // Revert the same commit a second time
                result = repo.Revert(
                    commitToRevert,
                    Constants.Signature,
                    new RevertOptions() { CommitOnSuccess = commitOnSuccess });

                Assert.NotNull(result);
                Assert.Null(result.Commit);
                Assert.Equal(RevertStatus.NothingToRevert, result.Status);

                if (commitOnSuccess)
                {
                    Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);
                }
                else
                {
                    Assert.Equal(CurrentOperation.Revert, repo.Info.CurrentOperation);
                }
            }
        }

        [Fact]
        public void RevertOrphanedBranchThrows()
        {
            // The branch name to perform the revert on
            const string revertBranchName = "refs/heads/revert";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                Commit commitToRevert = repo.Head.Tip;

                // Move the HEAD to an orphaned branch.
                repo.Refs.UpdateTarget("HEAD", "refs/heads/orphan");
                Assert.True(repo.Info.IsHeadUnborn);

                // Revert the tip of the refs/heads/revert branch.
                Assert.Throws<UnbornBranchException>(() => repo.Revert(commitToRevert, Constants.Signature));
            }
        }

        [Fact]
        public void RevertWithNothingToRevertInObjectDatabaseSucceeds()
        {
            // The branch name to perform the revert on
            const string revertBranchName = "refs/heads/revert";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch.
                Branch branch = Commands.Checkout(repo, revertBranchName);
                Assert.NotNull(branch);

                Commit commitToRevert = repo.Head.Tip;

                // Revert tip commit.
                RevertResult result = repo.Revert(commitToRevert, Constants.Signature);
                Assert.NotNull(result);
                Assert.Equal(RevertStatus.Reverted, result.Status);

                var revertResult = repo.ObjectDatabase.RevertCommit(commitToRevert, repo.Branches[revertBranchName].Tip, 0, null);

                Assert.NotNull(revertResult);
                Assert.Equal(MergeTreeStatus.Succeeded, revertResult.Status);
            }
        }

        [Fact]
        public void RevertWithConflictReportsConflict()
        {
            // The branch name to perform the revert on,
            // and the file whose contents we expect to be reverted.
            const string revertBranchName = "refs/heads/revert";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // The commit to revert - we know that reverting this
                // specific commit will generate conflicts.
                Commit commitToRevert = repo.Lookup<Commit>("cb4f7f0eca7a0114cdafd8537332aa17de36a4e9");
                Assert.NotNull(commitToRevert);

                // Perform the revert and verify there were conflicts.
                var result = repo.ObjectDatabase.RevertCommit(commitToRevert, repo.Branches[revertBranchName].Tip, 0, null);
                Assert.NotNull(result);
                Assert.Equal(MergeTreeStatus.Conflicts, result.Status);
                Assert.Null(result.Tree);
            }
        }

        [Fact]
        public void CanRevertInObjectDatabase()
        {
            // The branch name to perform the revert on
            const string revertBranchName = "refs/heads/revert";

            string path = SandboxRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Revert tip commit.
                var result = repo.ObjectDatabase.RevertCommit(repo.Branches[revertBranchName].Tip, repo.Branches[revertBranchName].Tip, 0, null);
                Assert.Equal(MergeTreeStatus.Succeeded, result.Status);
            }
        }
    }
}
