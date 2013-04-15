using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;
using System.IO;

namespace LibGit2Sharp.Tests
{
    public class CheckoutFixture : BaseFixture
    {
        private static readonly string originalFilePath = "a.txt";
        private static readonly string originalFileContent = "Hello";
        private static readonly string alternateFileContent = "There again";
        private static readonly string otherBranchName = "other";

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("diff-test-cases")]
        public void CanCheckoutAnExistingBranch(string branchName)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[branchName];
                Assert.NotNull(branch);

                Branch test = repo.Checkout(branch);
                Assert.False(repo.Info.IsHeadDetached);

                Assert.False(test.IsRemote);
                Assert.True(test.IsCurrentRepositoryHead);
                Assert.Equal(repo.Head, test);

                Assert.False(master.IsCurrentRepositoryHead);

                // Working directory should not be dirty
                Assert.False(repo.Index.RetrieveStatus().IsDirty);
            }
        }

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("diff-test-cases")]
        public void CanCheckoutAnExistingBranchByName(string branchName)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                Branch test = repo.Checkout(branchName);
                Assert.False(repo.Info.IsHeadDetached);

                Assert.False(test.IsRemote);
                Assert.True(test.IsCurrentRepositoryHead);
                Assert.Equal(repo.Head, test);

                Assert.False(master.IsCurrentRepositoryHead);

                // Working directory should not be dirty
                Assert.False(repo.Index.RetrieveStatus().IsDirty);
            }
        }

        [Theory]
        [InlineData("6dcf9bf")]
        [InlineData("refs/tags/lw")]
        [InlineData("HEAD~2")]
        public void CanCheckoutAnArbitraryCommit(string commitPointer)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                var commitSha = repo.Lookup(commitPointer).Sha;

                Branch detachedHead = repo.Checkout(commitPointer);

                Assert.Equal(repo.Head, detachedHead);
                Assert.Equal(commitSha, detachedHead.Tip.Sha);
                Assert.True(repo.Head.IsCurrentRepositoryHead);
                Assert.True(repo.Info.IsHeadDetached);
                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                Assert.True(detachedHead.IsCurrentRepositoryHead);
                Assert.False(detachedHead.IsRemote);
                Assert.Equal(detachedHead.Name, detachedHead.CanonicalName);

                Assert.Equal("(no branch)", detachedHead.CanonicalName);

                Assert.False(master.IsCurrentRepositoryHead);
            }
        }

        [Fact]
        public void CheckoutAddsMissingFilesInWorkingDirectory()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Remove the file in master branch
                // Verify it exists after checking out otherBranch.
                string fileFullPath = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                repo.Index.Remove(fileFullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                otherBranch.Checkout();

                // Verify working directory is updated
                Assert.False(repo.Index.RetrieveStatus().IsDirty);
                Assert.Equal(originalFileContent, File.ReadAllText(fileFullPath));
            }
        }

        [Fact]
        public void CheckoutRemovesExtraFilesInWorkingDirectory()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Add extra file in master branch
                // Verify it is removed after checking out otherBranch.
                string newFileFullPath = Path.Combine(repo.Info.WorkingDirectory, "b.txt");
                File.WriteAllText(newFileFullPath, "hello from master branch!\n");
                repo.Index.Stage(newFileFullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                otherBranch.Checkout();

                // Verify working directory is updated
                Assert.False(repo.Index.RetrieveStatus().IsDirty);
                Assert.False(File.Exists(newFileFullPath));
            }
        }

        [Fact]
        public void CheckoutUpdatesModifiedFilesInWorkingDirectory()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Modify file in master branch.
                // Verify contents match initial commit after checking out other branch.
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                File.WriteAllText(fullPath, "Update : hello from master branch!\n");
                repo.Index.Stage(fullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                otherBranch.Checkout();

                // Verify working directory is updated
                Assert.False(repo.Index.RetrieveStatus().IsDirty);
                Assert.Equal(originalFileContent, File.ReadAllText(fullPath));
            }
        }

        [Fact]
        public void CanForcefullyCheckoutWithConflictingStagedChanges()
        {
            // This test will check that we can checkout a branch that results
            // in a conflict. Here is the high level steps of the test:
            // 1) Create branch otherBranch from current commit in master,
            // 2) Commit change to master
            // 3) Switch to otherBranch
            // 4) Create conflicting change
            // 5) Forcefully checkout master

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string fileFullPath = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head.
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                // Create otherBranch from current Head.
                repo.Branches.Add(otherBranchName, master.Tip);

                // Add change to master.
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, fileFullPath);
                File.WriteAllText(fullPath, originalFileContent);
                repo.Index.Stage(fullPath);
                repo.Commit("change in master", Constants.Signature, Constants.Signature);

                // Checkout otherBranch.
                repo.Checkout(otherBranchName);

                // Add change to otherBranch.
                File.WriteAllText(fullPath, alternateFileContent);
                repo.Index.Stage(fullPath);

                // Assert that normal checkout throws exception
                // for the conflict.
                Assert.Throws<MergeConflictException>(() => repo.Checkout(master.CanonicalName));

                // Checkout with force option should succeed.
                repo.Checkout(master.CanonicalName, CheckoutOptions.Force, null);

                // Assert that master branch is checked out.
                Assert.True(repo.Branches["master"].IsCurrentRepositoryHead);

                // And that the current index is not dirty.
                Assert.False(repo.Index.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public void CheckingOutWithMergeConflictsThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, "a.txt");
                File.WriteAllText(fullPath, "Hello\n");
                repo.Index.Stage(fullPath);
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                // Create 2nd branch
                repo.CreateBranch("branch2");

                // Update file in main
                File.WriteAllText(fullPath, "Hello from master!\n");
                repo.Index.Stage(fullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout branch2
                repo.Checkout("branch2");
                File.WriteAllText(fullPath, "Hello From branch2!\n");

                // Assert that checking out master throws
                // when there are unstaged commits
                Assert.Throws<MergeConflictException>(() => repo.Checkout("master"));

                // And when there are staged commits
                repo.Index.Stage(fullPath);
                Assert.Throws<MergeConflictException>(() => repo.Checkout("master"));
            }
        }

        [Fact]
        public void CheckingOutInABareRepoThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Checkout(repo.Branches["refs/heads/test"]));
                Assert.Throws<BareRepositoryException>(() => repo.Checkout("refs/heads/test"));
            }
        }

        [Fact]
        public void CheckingOutAgainstAnUnbornBranchThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                Assert.True(repo.Info.IsHeadOrphaned);

                Assert.Throws<OrphanedHeadException>(() => repo.Checkout(repo.Head));
            }
        }

        [Fact]
        public void CheckingOutANonExistingBranchThrows()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Checkout("i-do-not-exist"));
            }
        }

        [Fact]
        public void CheckingOutABranchWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Checkout(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(Branch)));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(string)));
            }
        }

        [Fact]
        public void CheckingOutThroughBranchCallsCheckoutProgress()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);
                bool wasCalled = false;

                Branch branch = repo.Branches[otherBranchName];
                branch.Checkout(CheckoutOptions.None, (path, completed, total) => wasCalled = true);

                Assert.True(wasCalled);
            }
        }

        [Fact]
        public void CheckingOutThroughRepositoryCallsCheckoutProgress()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);
                bool wasCalled = false;

                repo.Checkout(otherBranchName, CheckoutOptions.None, (path, completed, total) => wasCalled = true);

                Assert.True(wasCalled);
            }
        }

        [Fact]
        public void CheckoutRetainsUntrackedChanges()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Generate an unstaged change.
                string fullPathFileB = Path.Combine(repo.Info.WorkingDirectory, "b.txt");
                File.WriteAllText(fullPathFileB, alternateFileContent);

                // Verify that there is an untracked entry.
                Assert.Equal(1, repo.Index.RetrieveStatus().Untracked.Count());
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(fullPathFileB));

                repo.Checkout(otherBranchName);

                // Verify untracked entry still exists.
                Assert.Equal(1, repo.Index.RetrieveStatus().Untracked.Count());
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(fullPathFileB));
            }
        }

        [Fact]
        public void ForceCheckoutRetainsUntrackedChanges()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Generate an unstaged change.
                string fullPathFileB = Path.Combine(repo.Info.WorkingDirectory, "b.txt");
                File.WriteAllText(fullPathFileB, alternateFileContent);

                // Verify that there is an untracked entry.
                Assert.Equal(1, repo.Index.RetrieveStatus().Untracked.Count());
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(fullPathFileB));

                repo.Checkout(otherBranchName, CheckoutOptions.Force, null);

                // Verify untracked entry still exists.
                Assert.Equal(1, repo.Index.RetrieveStatus().Untracked.Count());
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(fullPathFileB));
            }
        }

        [Fact]
        public void CheckoutRetainsUnstagedChanges()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Generate an unstaged change.
                string fullPathFileA = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                File.WriteAllText(fullPathFileA, alternateFileContent);

                // Verify that there is a modified entry.
                Assert.Equal(1, repo.Index.RetrieveStatus().Modified.Count());
                Assert.Equal(FileStatus.Modified, repo.Index.RetrieveStatus(fullPathFileA));

                repo.Checkout(otherBranchName);

                // Verify modified entry still exists.
                Assert.Equal(1, repo.Index.RetrieveStatus().Modified.Count());
                Assert.Equal(FileStatus.Modified, repo.Index.RetrieveStatus(fullPathFileA));
            }
        }

        [Fact]
        public void CheckoutRetainsStagedChanges()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Generate a staged change.
                string fullPathFileA = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                File.WriteAllText(fullPathFileA, alternateFileContent);
                repo.Index.Stage(fullPathFileA);

                // Verify that there is a staged entry.
                Assert.Equal(1, repo.Index.RetrieveStatus().Staged.Count());
                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus(fullPathFileA));

                repo.Checkout(otherBranchName);

                // Verify staged entry still exists.
                Assert.Equal(1, repo.Index.RetrieveStatus().Staged.Count());
                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus(fullPathFileA));
            }
        }

        [Fact]
        public void CheckoutRetainsIgnoredChanges()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Create a bin directory.
                string ignoredDirectoryPath = Path.Combine(repo.Info.WorkingDirectory, "bin");
                Directory.CreateDirectory(ignoredDirectoryPath);

                // Create file in ignored bin directory.
                string ignoredFilePath = Path.Combine(repo.Info.WorkingDirectory, Path.Combine("bin", "some_ignored_file.txt"));
                File.WriteAllText(ignoredFilePath, "hello from this ignored file.");

                // The following check does not report ignored entries...
                // TODO: Uncomment once libgit2/libgit2#1251 is merged
                // Assert.Equal(1, repo.Index.RetrieveStatus().Ignored.Count());

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(ignoredFilePath));

                repo.Checkout(otherBranchName);

                // Verify that the ignored file still exists.
                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(ignoredFilePath));
                Assert.True(File.Exists(ignoredFilePath));
            }
        }

        [Fact]
        public void ForceCheckoutRetainsIgnoredChanges()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Create a bin directory.
                string ignoredDirectoryPath = Path.Combine(repo.Info.WorkingDirectory, "bin");
                Directory.CreateDirectory(ignoredDirectoryPath);

                // Create file in ignored bin directory.
                string ignoredFilePath = Path.Combine(repo.Info.WorkingDirectory, Path.Combine("bin", "some_ignored_file.txt"));
                File.WriteAllText(ignoredFilePath, "hello from this ignored file.");

                // The following check does not report ignored entries...
                // TODO: Uncomment once libgit2/libgit2#1251 is merged
                // Assert.Equal(1, repo.Index.RetrieveStatus().Ignored.Count());

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(ignoredFilePath));

                repo.Checkout(otherBranchName, CheckoutOptions.Force, null);

                // Verify that the ignored file still exists.
                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(ignoredFilePath));
                Assert.True(File.Exists(ignoredFilePath));
            }
        }

        [Fact]
        public void CheckoutBranchSnapshot()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                PopulateBasicRepository(repo);

                // Get the current status of master
                // and the current tip.
                Branch initial = repo.Branches["master"];
                Commit initialCommit = initial.Tip;

                // Add commit to master
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                File.WriteAllText(fullPath, "Update : hello from master branch!\n");
                repo.Index.Stage(fullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                Assert.False(repo.Info.IsHeadDetached);

                initial.Checkout();

                // Head should point at initial commit.
                Assert.Equal(repo.Head.Tip, initialCommit);
                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                // Verify that HEAD is detached.
                Assert.Equal(repo.Refs["HEAD"].TargetIdentifier, initial.Tip.Sha);
                Assert.True(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void CheckingOutRemoteBranchResultsInDetachedHead()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                repo.Checkout("refs/remotes/origin/master");

                // Verify that HEAD is detached.
                Assert.Equal(repo.Refs["HEAD"].TargetIdentifier, repo.Branches["origin/master"].Tip.Sha);
                Assert.True(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void CheckingOutABranchDoesNotAlterBinaryFiles()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // $ git hash-object square-logo.png
                // b758c5bc1c8117c2a4c545dae2903e36360501c5
                const string expectedSha = "b758c5bc1c8117c2a4c545dae2903e36360501c5";

                // The blob actually exists in the object database with the correct Sha
                Assert.Equal(expectedSha, repo.Lookup<Blob>(expectedSha).Sha);

                repo.Checkout("refs/heads/logo", CheckoutOptions.Force, null);

                // The Index has been updated as well with the blob
                Assert.Equal(expectedSha, repo.Index["square-logo.png"].Id.Sha);

                // Recreating a Blob from the checked out file...
                Blob blob = repo.ObjectDatabase.CreateBlob("square-logo.png");

                // ...generates the same Sha
                Assert.Equal(expectedSha, blob.Id.Sha);
            }
        }

        /// <summary>
        ///   Helper method to populate a simple repository with
        ///   a single file and two branches.
        /// </summary>
        /// <param name="repo">Repository to populate</param>
        private void PopulateBasicRepository(Repository repo)
        {
            // Generate a .gitignore file.
            string gitIgnoreFilePath = Path.Combine(repo.Info.WorkingDirectory, ".gitignore");
            File.WriteAllText(gitIgnoreFilePath, "bin");
            repo.Index.Stage(gitIgnoreFilePath);

            string fullPathFileA = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
            File.WriteAllText(fullPathFileA, originalFileContent);
            repo.Index.Stage(fullPathFileA);

            repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

            repo.CreateBranch(otherBranchName);
        }

        /// <summary>
        /// Reset and clean current working directory. This will ensure that the current
        /// working directory matches the current Head commit.
        /// </summary>
        /// <param name="repo">Repository whose current working directory should be operated on.</param>
        private void ResetAndCleanWorkingDirectory(Repository repo)
        {
            // Reset the index and the working tree.
            repo.Reset(ResetOptions.Hard);

            // Clean the working directory.
            repo.RemoveUntrackedFiles();
        }
    }
}
