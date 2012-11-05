using System;
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
        private static readonly string otherBranchName = "other";

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("diff-test-cases")]
        public void CanCheckoutAnExistingBranch(string branchName)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Hard reset to ensure that working directory, index, and HEAD match
                repo.Reset(ResetOptions.Hard);
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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Hard reset to ensure that working directory, index, and HEAD match
                repo.Reset(ResetOptions.Hard);
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
        public void CanCheckoutAnArbitraryCommit(string commitPointer)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Hard reset to ensure that working directory, index, and HEAD match
                repo.Reset(ResetOptions.Hard);
                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                Branch detachedHead = repo.Checkout(commitPointer);

                Assert.Equal(repo.Head, detachedHead);
                Assert.Equal(repo.Lookup(commitPointer).Sha, detachedHead.Tip.Sha);
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
        public void CanForcefullyCheckoutWithStagedChanges()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                string fileFullPath = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Hard reset to ensure that working directory, index, and HEAD match
                repo.Reset(ResetOptions.Hard);
                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                // Add local change
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, fileFullPath);
                File.WriteAllText(fullPath, originalFileContent);
                repo.Index.Stage(fullPath);

                // Verify working directory is now dirty
                Assert.True(repo.Index.RetrieveStatus().IsDirty);

                // And that the new file exists
                Assert.True(File.Exists(fileFullPath));

                // Checkout with the force option
                Branch targetBranch = repo.Branches["i-do-numbers"];
                targetBranch.Checkout(CheckoutOptions.Force, null);

                // Assert that target branch is checked out
                Assert.True(targetBranch.IsCurrentRepositoryHead);

                // And that staged change (add) is no longer preset
                Assert.False(File.Exists(fileFullPath));
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
                Assert.Throws<InvalidOperationException>(() => repo.Checkout(repo.Branches["refs/heads/test"]));
                Assert.Throws<InvalidOperationException>(() => repo.Checkout("refs/heads/test"));
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

        /// <summary>
        ///   Helper method to populate a simple repository with
        ///   a single file and two branches.
        /// </summary>
        /// <param name="repo">Repository to populate</param>
        private void PopulateBasicRepository(Repository repo)
        {
            string fullPathFileA = Path.Combine(repo.Info.WorkingDirectory, "a.txt");
            File.WriteAllText(fullPathFileA, originalFileContent);
            repo.Index.Stage(fullPathFileA);
            repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

            repo.CreateBranch(otherBranchName);
        }
    }
}
