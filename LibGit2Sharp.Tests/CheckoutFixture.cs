using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CheckoutFixture : BaseFixture
    {
        private const string originalFilePath = "a.txt";
        private const string originalFileContent = "Hello";
        private const string alternateFileContent = "There again";
        private const string otherBranchName = "other";

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("diff-test-cases")]
        public void CanCheckoutAnExistingBranch(string branchName)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch branch = repo.Branches[branchName];
                Assert.NotNull(branch);
                AssertBelongsToARepository(repo, branch);

                Branch test = Commands.Checkout(repo, branch);
                Assert.False(repo.Info.IsHeadDetached);
                AssertBelongsToARepository(repo, test);

                Assert.False(test.IsRemote);
                Assert.True(test.IsCurrentRepositoryHead);
                Assert.Equal(repo.Head, test);

                Assert.False(master.IsCurrentRepositoryHead);

                // Working directory should not be dirty
                Assert.False(repo.RetrieveStatus().IsDirty);

                // Assert reflog entry is created
                var reflogEntry = repo.Refs.Log(repo.Refs.Head).First();
                Assert.Equal(master.Tip.Id, reflogEntry.From);
                Assert.Equal(branch.Tip.Id, reflogEntry.To);
                Assert.NotNull(reflogEntry.Committer.Email);
                Assert.NotNull(reflogEntry.Committer.Name);
                Assert.Equal(string.Format("checkout: moving from master to {0}", branchName), reflogEntry.Message);
            }
        }

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("diff-test-cases")]
        public void CanCheckoutAnExistingBranchByName(string branchName)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch test = Commands.Checkout(repo, branchName);
                Assert.False(repo.Info.IsHeadDetached);

                Assert.False(test.IsRemote);
                Assert.True(test.IsCurrentRepositoryHead);
                Assert.Equal(repo.Head, test);

                Assert.False(master.IsCurrentRepositoryHead);

                // Working directory should not be dirty
                Assert.False(repo.RetrieveStatus().IsDirty);

                // Assert reflog entry is created
                var reflogEntry = repo.Refs.Log(repo.Refs.Head).First();
                Assert.Equal(master.Tip.Id, reflogEntry.From);
                Assert.Equal(repo.Branches[branchName].Tip.Id, reflogEntry.To);
                Assert.NotNull(reflogEntry.Committer.Email);
                Assert.NotNull(reflogEntry.Committer.Name);
                Assert.Equal(string.Format("checkout: moving from master to {0}", branchName), reflogEntry.Message);
            }
        }

        [Theory]
        [InlineData("6dcf9bf", true, "6dcf9bf")]
        [InlineData("refs/tags/lw", true, "lw")]
        [InlineData("HEAD~2", true, "HEAD~2")]
        [InlineData("6dcf9bf", false, "6dcf9bf7541ee10456529833502442f385010c3d")]
        [InlineData("refs/tags/lw", false, "e90810b8df3e80c413d903f631643c716887138d")]
        [InlineData("HEAD~2", false, "4c062a6361ae6959e06292c1fa5e2822d9c96345")]
        public void CanCheckoutAnArbitraryCommit(string commitPointer, bool checkoutByCommitOrBranchSpec, string expectedReflogTarget)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.RetrieveStatus().IsDirty);

                var commit = repo.Lookup<Commit>(commitPointer);
                AssertBelongsToARepository(repo, commit);

                Branch detachedHead = checkoutByCommitOrBranchSpec ? Commands.Checkout(repo, commitPointer) : Commands.Checkout(repo, commit);

                Assert.Equal(repo.Head, detachedHead);
                Assert.Equal(commit.Sha, detachedHead.Tip.Sha);
                Assert.True(repo.Head.IsCurrentRepositoryHead);
                Assert.True(repo.Info.IsHeadDetached);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Assert.True(detachedHead.IsCurrentRepositoryHead);
                Assert.False(detachedHead.IsRemote);
                Assert.Equal(detachedHead.FriendlyName, detachedHead.CanonicalName);

                Assert.Equal("(no branch)", detachedHead.CanonicalName);

                Assert.False(master.IsCurrentRepositoryHead);

                // Assert reflog entry is created
                var reflogEntry = repo.Refs.Log(repo.Refs.Head).First();
                Assert.Equal(master.Tip.Id, reflogEntry.From);
                Assert.Equal(commit.Sha, reflogEntry.To.Sha);
                Assert.NotNull(reflogEntry.Committer.Email);
                Assert.NotNull(reflogEntry.Committer.Name);
                Assert.Equal(string.Format("checkout: moving from master to {0}", expectedReflogTarget), reflogEntry.Message);
            }
        }

        [Fact]
        public void CheckoutAddsMissingFilesInWorkingDirectory()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Remove the file in master branch
                // Verify it exists after checking out otherBranch.
                string fileFullPath = Path.Combine(repo.Info.WorkingDirectory, originalFilePath);
                Commands.Remove(repo, fileFullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                Commands.Checkout(repo, otherBranch);

                // Verify working directory is updated
                Assert.False(repo.RetrieveStatus().IsDirty);
                Assert.Equal(originalFileContent, File.ReadAllText(fileFullPath));
            }
        }

        [Fact]
        public void CheckoutRemovesExtraFilesInWorkingDirectory()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Add extra file in master branch
                // Verify it is removed after checking out otherBranch.
                string newFileFullPath = Touch(
                    repo.Info.WorkingDirectory, "b.txt", "hello from master branch!\n");

                Commands.Stage(repo, newFileFullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                Commands.Checkout(repo, otherBranch);

                // Verify working directory is updated
                Assert.False(repo.RetrieveStatus().IsDirty);
                Assert.False(File.Exists(newFileFullPath));
            }
        }

        [Fact]
        public void CheckoutUpdatesModifiedFilesInWorkingDirectory()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Modify file in master branch.
                // Verify contents match initial commit after checking out other branch.
                string fullPath = Touch(
                    repo.Info.WorkingDirectory, originalFilePath, "Update : hello from master branch!\n");

                Commands.Stage(repo, fullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                Commands.Checkout(repo, otherBranch);

                // Verify working directory is updated
                Assert.False(repo.RetrieveStatus().IsDirty);
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

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head.
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.RetrieveStatus().IsDirty);

                // Create otherBranch from current Head.
                repo.Branches.Add(otherBranchName, master.Tip);

                // Add change to master.
                Touch(repo.Info.WorkingDirectory, originalFilePath, originalFileContent);

                Commands.Stage(repo, originalFilePath);
                repo.Commit("change in master", Constants.Signature, Constants.Signature);

                // Checkout otherBranch.
                Commands.Checkout(repo, otherBranchName);

                // Add change to otherBranch.
                Touch(repo.Info.WorkingDirectory, originalFilePath, alternateFileContent);
                Commands.Stage(repo, originalFilePath);

                // Assert that normal checkout throws exception
                // for the conflict.
                Assert.Throws<CheckoutConflictException>(() => Commands.Checkout(repo, master.CanonicalName));

                // Checkout with force option should succeed.
                Commands.Checkout(repo, master.CanonicalName, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });

                // Assert that master branch is checked out.
                Assert.True(repo.Branches["master"].IsCurrentRepositoryHead);

                // And that the current index is not dirty.
                Assert.False(repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public void CheckingOutWithMergeConflictsThrows()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.WorkingDirectory, originalFilePath, "Hello\n");
                Commands.Stage(repo, originalFilePath);
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                // Create 2nd branch
                repo.CreateBranch("branch2");

                // Update file in main
                Touch(repo.Info.WorkingDirectory, originalFilePath, "Hello from master!\n");
                Commands.Stage(repo, originalFilePath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout branch2
                Commands.Checkout(repo, "branch2");
                Touch(repo.Info.WorkingDirectory, originalFilePath, "Hello From branch2!\n");

                // Assert that checking out master throws
                // when there are unstaged commits
                Assert.Throws<CheckoutConflictException>(() => Commands.Checkout(repo, "master"));

                // And when there are staged commits
                Commands.Stage(repo, originalFilePath);
                Assert.Throws<CheckoutConflictException>(() => Commands.Checkout(repo, "master"));
            }
        }

        [Fact]
        public void CanCancelCheckoutThroughNotifyCallback()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                const string relativePath = "a.txt";
                Touch(repo.Info.WorkingDirectory, relativePath, "Hello\n");

                Commands.Stage(repo, relativePath);
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                // Create 2nd branch
                repo.CreateBranch("branch2");

                // Update file in main
                Touch(repo.Info.WorkingDirectory, relativePath, "Hello from master!\n");
                Commands.Stage(repo, relativePath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout branch2
                Commands.Checkout(repo, "branch2");

                // Update the context of a.txt - a.txt will then conflict between branch2 and master.
                Touch(repo.Info.WorkingDirectory, relativePath, "Hello From branch2!\n");

                // Verify that we get called for the notify conflict cb
                string conflictPath = string.Empty;

                CheckoutOptions options = new CheckoutOptions()
                {
                    OnCheckoutNotify = (path, flags) => { conflictPath = path; return false; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict,
                };

                Assert.Throws<UserCancelledException>(() => Commands.Checkout(repo, "master", options));
                Assert.Equal(relativePath, conflictPath);
            }
        }

        [Fact]
        public void CheckingOutInABareRepoThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<BareRepositoryException>(() => Commands.Checkout(repo, repo.Branches["refs/heads/test"]));
                Assert.Throws<BareRepositoryException>(() => Commands.Checkout(repo, "refs/heads/test"));
            }
        }

        [Fact]
        public void CheckingOutAgainstAnUnbornBranchThrows()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.True(repo.Info.IsHeadUnborn);

                Assert.Throws<UnbornBranchException>(() => Commands.Checkout(repo, repo.Head));
            }
        }

        [Fact]
        public void CheckingOutANonExistingBranchThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => Commands.Checkout(repo, "i-do-not-exist"));
            }
        }

        [Fact]
        public void CheckingOutABranchWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => Commands.Checkout(repo, string.Empty));
                Assert.Throws<ArgumentNullException>(() => Commands.Checkout(repo, default(Branch)));
                Assert.Throws<ArgumentNullException>(() => Commands.Checkout(repo, default(string)));
            }
        }

        [Fact]
        public void CheckingOutThroughBranchCallsCheckoutProgress()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);
                bool wasCalled = false;

                Branch branch = repo.Branches[otherBranchName];
                Commands.Checkout(repo, branch,
                    new CheckoutOptions { OnCheckoutProgress = (path, completed, total) => wasCalled = true });

                Assert.True(wasCalled);
            }
        }

        [Fact]
        public void CheckingOutThroughRepositoryCallsCheckoutProgress()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);
                bool wasCalled = false;

                Commands.Checkout(repo, otherBranchName, new CheckoutOptions() { OnCheckoutProgress = (path, completed, total) => wasCalled = true });

                Assert.True(wasCalled);
            }
        }

        [Theory]
        [InlineData(CheckoutNotifyFlags.Conflict, "conflict.txt", false)]
        [InlineData(CheckoutNotifyFlags.Updated, "updated.txt", false)]
        [InlineData(CheckoutNotifyFlags.Untracked, "untracked.txt", false)]
        [InlineData(CheckoutNotifyFlags.Ignored, "bin", true)]
        public void CheckingOutCallsCheckoutNotify(CheckoutNotifyFlags notifyFlags, string expectedNotificationPath, bool isDirectory)
        {
            if (isDirectory)
            {
                expectedNotificationPath = expectedNotificationPath + Path.DirectorySeparatorChar;
            }

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                const string relativePathUpdated = "updated.txt";
                Touch(repo.Info.WorkingDirectory, relativePathUpdated, "updated file text A");
                Commands.Stage(repo, relativePathUpdated);
                repo.Commit("Commit initial update file", Constants.Signature, Constants.Signature);

                // Create conflicting change
                const string relativePathConflict = "conflict.txt";
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text A");
                Commands.Stage(repo, relativePathConflict);
                repo.Commit("Initial commit of conflict.txt and update.txt", Constants.Signature, Constants.Signature);

                // Create another branch
                repo.CreateBranch("newbranch");

                // Make an edit to conflict.txt and update.txt
                Touch(repo.Info.WorkingDirectory, relativePathUpdated, "updated file text BB");
                Commands.Stage(repo, relativePathUpdated);
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text BB");
                Commands.Stage(repo, relativePathConflict);

                repo.Commit("2nd commit of conflict.txt and update.txt on master branch", Constants.Signature, Constants.Signature);

                // Checkout other branch
                Commands.Checkout(repo, "newbranch");

                // Make alternate edits to conflict.txt and update.txt
                Touch(repo.Info.WorkingDirectory, relativePathUpdated, "updated file text CCC");
                Commands.Stage(repo, relativePathUpdated);
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text CCC");
                Commands.Stage(repo, relativePathConflict);
                repo.Commit("2nd commit of conflict.txt and update.txt on newbranch", Constants.Signature, Constants.Signature);

                // make conflicting change to conflict.txt
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text DDDD");
                Commands.Stage(repo, relativePathConflict);

                // Create ignored change
                string relativePathIgnore = Path.Combine("bin", "ignored.txt");
                Touch(repo.Info.WorkingDirectory, relativePathIgnore, "ignored file");

                // Create untracked change
                const string relativePathUntracked = "untracked.txt";
                Touch(repo.Info.WorkingDirectory, relativePathUntracked, "untracked file");

                bool wasCalled = false;
                string actualNotificationPath = string.Empty;
                CheckoutNotifyFlags actualNotifyFlags = CheckoutNotifyFlags.None;

                CheckoutOptions options = new CheckoutOptions()
                {
                    OnCheckoutNotify = (path, notificationType) => { wasCalled = true; actualNotificationPath = path; actualNotifyFlags = notificationType; return true; },
                    CheckoutNotifyFlags = notifyFlags,
                };

                Assert.Throws<CheckoutConflictException>(() => Commands.Checkout(repo, "master", options));

                Assert.True(wasCalled);
                Assert.Equal(expectedNotificationPath, actualNotificationPath);
                Assert.Equal(notifyFlags, actualNotifyFlags);
            }
        }

        [Fact]
        public void CheckoutRetainsUntrackedChanges()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Generate an unstaged change.
                string fullPathFileB = Touch(repo.Info.WorkingDirectory, "b.txt", alternateFileContent);

                // Verify that there is an untracked entry.
                Assert.Single(repo.RetrieveStatus().Untracked);
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(fullPathFileB));

                Commands.Checkout(repo, otherBranchName);

                // Verify untracked entry still exists.
                Assert.Single(repo.RetrieveStatus().Untracked);
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(fullPathFileB));
            }
        }

        [Fact]
        public void ForceCheckoutRetainsUntrackedChanges()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Generate an unstaged change.
                string fullPathFileB = Touch(repo.Info.WorkingDirectory, "b.txt", alternateFileContent);

                // Verify that there is an untracked entry.
                Assert.Single(repo.RetrieveStatus().Untracked);
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(fullPathFileB));

                Commands.Checkout(repo, otherBranchName, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });

                // Verify untracked entry still exists.
                Assert.Single(repo.RetrieveStatus().Untracked);
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(fullPathFileB));
            }
        }

        [Fact]
        public void CheckoutRetainsUnstagedChanges()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Generate an unstaged change.
                string fullPathFileA = Touch(repo.Info.WorkingDirectory, originalFilePath, alternateFileContent);

                // Verify that there is a modified entry.
                Assert.Single(repo.RetrieveStatus().Modified);
                Assert.Equal(FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(fullPathFileA));

                Commands.Checkout(repo, otherBranchName);

                // Verify modified entry still exists.
                Assert.Single(repo.RetrieveStatus().Modified);
                Assert.Equal(FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(fullPathFileA));
            }
        }

        [Fact]
        public void CheckoutRetainsStagedChanges()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Generate a staged change.
                string fullPathFileA = Touch(repo.Info.WorkingDirectory, originalFilePath, alternateFileContent);
                Commands.Stage(repo, fullPathFileA);

                // Verify that there is a staged entry.
                Assert.Single(repo.RetrieveStatus().Staged);
                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus(fullPathFileA));

                Commands.Checkout(repo, otherBranchName);

                // Verify staged entry still exists.
                Assert.Single(repo.RetrieveStatus().Staged);
                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus(fullPathFileA));
            }
        }

        [Fact]
        public void CheckoutRetainsIgnoredChanges()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Create file in ignored bin directory.
                string ignoredFilePath = Touch(
                    repo.Info.WorkingDirectory,
                    "bin/some_ignored_file.txt",
                    "hello from this ignored file.");

                Assert.Single(repo.RetrieveStatus(new StatusOptions { IncludeIgnored = true }).Ignored);

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(ignoredFilePath));

                Commands.Checkout(repo, otherBranchName);

                // Verify that the ignored file still exists.
                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(ignoredFilePath));
                Assert.True(File.Exists(ignoredFilePath));
            }
        }

        [Fact]
        public void ForceCheckoutRetainsIgnoredChanges()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Create file in ignored bin directory.
                string ignoredFilePath = Touch(
                    repo.Info.WorkingDirectory,
                    "bin/some_ignored_file.txt",
                    "hello from this ignored file.");

                Assert.Single(repo.RetrieveStatus(new StatusOptions { IncludeIgnored = true }).Ignored);

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(ignoredFilePath));

                Commands.Checkout(repo, otherBranchName, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });

                // Verify that the ignored file still exists.
                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(ignoredFilePath));
                Assert.True(File.Exists(ignoredFilePath));
            }
        }

        [Fact]
        public void CheckoutBranchSnapshot()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                PopulateBasicRepository(repo);

                // Get the current status of master
                // and the current tip.
                Branch initial = repo.Branches["master"];
                Commit initialCommit = initial.Tip;

                // Add commit to master
                string fullPath = Touch(repo.Info.WorkingDirectory, originalFilePath, "Update : hello from master branch!\n");
                Commands.Stage(repo, fullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                Assert.False(repo.Info.IsHeadDetached);

                Commands.Checkout(repo, initial);

                // Head should point at initial commit.
                Assert.Equal(repo.Head.Tip, initialCommit);
                Assert.False(repo.RetrieveStatus().IsDirty);

                // Verify that HEAD is detached.
                Assert.Equal(repo.Refs["HEAD"].TargetIdentifier, initial.Tip.Sha);
                Assert.True(repo.Info.IsHeadDetached);
            }
        }

        [Theory]
        [InlineData("refs/remotes/origin/master")]
        [InlineData("master@{u}")]
        [InlineData("origin/master")]
        public void CheckingOutRemoteBranchResultsInDetachedHead(string remoteBranchSpec)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Commands.Checkout(repo, remoteBranchSpec);

                // Verify that HEAD is detached.
                Assert.Equal(repo.Refs["HEAD"].TargetIdentifier, repo.Branches["origin/master"].Tip.Sha);
                Assert.True(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void CheckingOutABranchDoesNotAlterBinaryFiles()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // $ git hash-object square-logo.png
                // b758c5bc1c8117c2a4c545dae2903e36360501c5
                const string expectedSha = "b758c5bc1c8117c2a4c545dae2903e36360501c5";

                // The blob actually exists in the object database with the correct Sha
                Assert.Equal(expectedSha, repo.Lookup<Blob>(expectedSha).Sha);

                Commands.Checkout(repo, "refs/heads/logo", new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });

                // The Index has been updated as well with the blob
                Assert.Equal(expectedSha, repo.Index["square-logo.png"].Id.Sha);

                // Recreating a Blob from the checked out file...
                Blob blob = repo.ObjectDatabase.CreateBlob("square-logo.png");

                // ...generates the same Sha
                Assert.Equal(expectedSha, blob.Id.Sha);
            }
        }

        [Theory]
        [InlineData("a447ba2ca8")]
        [InlineData("lw")]
        [InlineData("e90810^{}")]
        public void CheckoutFromDetachedHead(string commitPointer)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                var commitSha = repo.Lookup(commitPointer).Sha;

                Branch initialHead = Commands.Checkout(repo, "6dcf9bf");

                Commands.Checkout(repo, commitPointer);

                // Assert reflog entry is created
                var reflogEntry = repo.Refs.Log(repo.Refs.Head).First();
                Assert.Equal(initialHead.Tip.Id, reflogEntry.From);
                Assert.Equal(commitSha, reflogEntry.To.Sha);
                Assert.NotNull(reflogEntry.Committer.Email);
                Assert.NotNull(reflogEntry.Committer.Name);
                Assert.Equal(string.Format("checkout: moving from {0} to {1}", initialHead.Tip.Sha, commitPointer), reflogEntry.Message);
            }
        }

        [Fact]
        public void CheckoutBranchFromDetachedHead()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch initialHead = Commands.Checkout(repo, "6dcf9bf");

                Assert.True(repo.Info.IsHeadDetached);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newHead = Commands.Checkout(repo, repo.Branches["master"]);

                // Assert reflog entry is created
                AssertRefLogEntry(repo, "HEAD",
                    string.Format("checkout: moving from {0} to {1}", initialHead.Tip.Sha, newHead.FriendlyName),
                    initialHead.Tip.Id, newHead.Tip.Id, Constants.Identity, before);
            }
        }

        [Theory]
        [InlineData("master", "refs/heads/master")]
        [InlineData("heads/master", "refs/heads/master")]
        public void CheckoutBranchByShortNameAttachesTheHead(string shortBranchName, string referenceName)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Commands.Checkout(repo, "6dcf9bf");
                Assert.True(repo.Info.IsHeadDetached);

                var branch = Commands.Checkout(repo, shortBranchName);

                Assert.False(repo.Info.IsHeadDetached);
                Assert.Equal(referenceName, repo.Head.CanonicalName);
                Assert.Equal(referenceName, branch.CanonicalName);
            }
        }

        [Fact]
        public void CheckoutPreviousCheckedOutBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch previousHead = Commands.Checkout(repo, "i-do-numbers");
                Commands.Checkout(repo, "diff-test-cases");

                //Go back to previous branch checked out
                var branch = Commands.Checkout(repo, @"@{-1}");

                Assert.False(repo.Info.IsHeadDetached);
                Assert.Equal(previousHead.CanonicalName, repo.Head.CanonicalName);
                Assert.Equal(previousHead.CanonicalName, branch.CanonicalName);
            }
        }

        [Fact]
        public void CheckoutCurrentReference()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                var reflogEntriesCount = repo.Refs.Log(repo.Refs.Head).Count();

                // Checkout branch
                Commands.Checkout(repo, master);

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                // Checkout in detached mode
                Commands.Checkout(repo, master.Tip.Sha);

                Assert.True(repo.Info.IsHeadDetached);
                AssertRefLogEntry(repo, "HEAD",
                    string.Format("checkout: moving from master to {0}", master.Tip.Sha),
                    master.Tip.Id, master.Tip.Id, Constants.Identity, before);

                // Checkout detached "HEAD" => nothing should happen
                reflogEntriesCount = repo.Refs.Log(repo.Refs.Head).Count();

                Commands.Checkout(repo, repo.Head);

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());

                // Checkout attached "HEAD" => nothing should happen
                Commands.Checkout(repo, "master");
                reflogEntriesCount = repo.Refs.Log(repo.Refs.Head).Count();

                Commands.Checkout(repo, repo.Head);

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());

                Commands.Checkout(repo, "HEAD");

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());
            }
        }

        [Fact]
        public void CheckoutLowerCasedHeadThrows()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => Commands.Checkout(repo, "head"));
            }
        }

        [Fact]
        public void CanCheckoutAttachedHead()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.False(repo.Info.IsHeadDetached);

                Commands.Checkout(repo, repo.Head);
                Assert.False(repo.Info.IsHeadDetached);

                Commands.Checkout(repo, "HEAD");
                Assert.False(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void CanCheckoutDetachedHead()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Commands.Checkout(repo, repo.Head.Tip.Sha);

                Assert.True(repo.Info.IsHeadDetached);

                Commands.Checkout(repo, repo.Head);
                Assert.True(repo.Info.IsHeadDetached);

                Commands.Checkout(repo, "HEAD");
                Assert.True(repo.Info.IsHeadDetached);
            }
        }

        [Theory]
        [InlineData("master", "6dcf9bf", "readme.txt", FileStatus.NewInIndex)]
        [InlineData("master", "refs/tags/lw", "readme.txt", FileStatus.NewInIndex)]
        [InlineData("master", "i-do-numbers", "super-file.txt", FileStatus.NewInIndex)]
        [InlineData("i-do-numbers", "diff-test-cases", "numbers.txt", FileStatus.ModifiedInIndex)]
        public void CanCheckoutPath(string originalBranch, string checkoutFrom, string path, FileStatus expectedStatus)
        {
            string repoPath = SandboxStandardTestRepo();
            using (var repo = new Repository(repoPath))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Commands.Checkout(repo, originalBranch);
                Assert.False(repo.RetrieveStatus().IsDirty);

                repo.CheckoutPaths(checkoutFrom, new[] { path });

                Assert.Equal(expectedStatus, repo.RetrieveStatus(path));
                Assert.Single(repo.RetrieveStatus());
            }
        }

        [Fact]
        public void CanCheckoutPaths()
        {
            string repoPath = SandboxStandardTestRepo();
            var checkoutPaths = new[] { "numbers.txt", "super-file.txt" };

            using (var repo = new Repository(repoPath))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                repo.CheckoutPaths("i-do-numbers", checkoutPaths);

                foreach (string checkoutPath in checkoutPaths)
                {
                    Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(checkoutPath));
                }
            }
        }

        [Fact]
        public void CannotCheckoutPathsWithEmptyOrNullPathArgument()
        {
            string repoPath = SandboxStandardTestRepo();

            using (var repo = new Repository(repoPath))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                // Passing null 'paths' parameter should throw
                Assert.Throws<ArgumentNullException>(() => repo.CheckoutPaths("i-do-numbers", null));

                // Passing empty list should do nothing
                repo.CheckoutPaths("i-do-numbers", Enumerable.Empty<string>());
                Assert.False(repo.RetrieveStatus().IsDirty);
            }
        }

        [Theory]
        [InlineData("new.txt")]
        [InlineData("1.txt")]
        public void CanCheckoutPathFromCurrentBranch(string fileName)
        {
            string repoPath = SandboxStandardTestRepo();

            using (var repo = new Repository(repoPath))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);

                Assert.False(repo.RetrieveStatus().IsDirty);

                Touch(repo.Info.WorkingDirectory, fileName, "new text file");

                Assert.True(repo.RetrieveStatus().IsDirty);

                var opts = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
                repo.CheckoutPaths("HEAD", new[] { fileName }, opts);

                Assert.False(repo.RetrieveStatus().IsDirty);
            }
        }

        [Theory]
        [InlineData("br2", "origin")]
        [InlineData("unique/branch", "another/remote")]
        public void CheckoutBranchTriesRemoteTrackingBranchAsFallbackAndSucceedsIfOnlyOne(string branchName, string expectedRemoteName)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                ResetAndCleanWorkingDirectory(repo);

                // Define another remote
                var otherRemote = "another/remote";
                repo.Network.Remotes.Add(otherRemote, "https://github.com/libgit2/TestGitRepository");

                // Define an extra remote tracking branch that does not conflict
                repo.Refs.Add($"refs/remotes/{otherRemote}/unique/branch", repo.Head.Tip.Sha);

                Branch branch = Commands.Checkout(repo, branchName);

                Assert.NotNull(branch);
                Assert.True(branch.IsTracking);
                Assert.Equal($"refs/remotes/{expectedRemoteName}/{branchName}", branch.TrackedBranch.CanonicalName);
            }
        }

        [Fact]
        public void CheckoutBranchTriesRemoteTrackingBranchAsFallbackAndThrowsIfMoreThanOne()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                ResetAndCleanWorkingDirectory(repo);

                // Define another remote
                var otherRemote = "another/remote";
                repo.Network.Remotes.Add(otherRemote, "https://github.com/libgit2/TestGitRepository");

                // Define remote tracking branches that conflict
                var branchName = "conflicting/branch";
                repo.Refs.Add($"refs/remotes/origin/{branchName}", repo.Head.Tip.Sha);
                repo.Refs.Add($"refs/remotes/{otherRemote}/{branchName}", repo.Head.Tip.Sha);

                Assert.Throws<AmbiguousSpecificationException>(() => Commands.Checkout(repo, branchName));
            }
        }

        /// <summary>
        /// Helper method to populate a simple repository with
        /// a single file and two branches.
        /// </summary>
        /// <param name="repo">Repository to populate</param>
        private void PopulateBasicRepository(IRepository repo)
        {
            // Generate a .gitignore file.
            string gitIgnoreFilePath = Touch(repo.Info.WorkingDirectory, ".gitignore", "bin");
            Commands.Stage(repo, gitIgnoreFilePath);

            string fullPathFileA = Touch(repo.Info.WorkingDirectory, originalFilePath, originalFileContent);
            Commands.Stage(repo, fullPathFileA);

            repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

            repo.CreateBranch(otherBranchName);
        }

        /// <summary>
        /// Reset and clean current working directory. This will ensure that the current
        /// working directory matches the current Head commit.
        /// </summary>
        /// <param name="repo">Repository whose current working directory should be operated on.</param>
        private void ResetAndCleanWorkingDirectory(IRepository repo)
        {
            // Reset the index and the working tree.
            repo.Reset(ResetMode.Hard);

            // Clean the working directory.
            repo.RemoveUntrackedFiles();
        }
    }
}
