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

                Branch test = repo.Checkout(branch);
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

                Branch test = repo.Checkout(branchName);
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
        [InlineData("refs/tags/lw", true, "refs/tags/lw")]
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

                Branch detachedHead = checkoutByCommitOrBranchSpec ? repo.Checkout(commitPointer) : repo.Checkout(commit);

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
                repo.Remove(fileFullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                repo.Checkout(otherBranch);

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

                repo.Stage(newFileFullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                repo.Checkout(otherBranch);

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

                repo.Stage(fullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout other_branch
                Branch otherBranch = repo.Branches[otherBranchName];
                Assert.NotNull(otherBranch);
                repo.Checkout(otherBranch);

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

                repo.Stage(originalFilePath);
                repo.Commit("change in master", Constants.Signature, Constants.Signature);

                // Checkout otherBranch.
                repo.Checkout(otherBranchName);

                // Add change to otherBranch.
                Touch(repo.Info.WorkingDirectory, originalFilePath, alternateFileContent);
                repo.Stage(originalFilePath);

                // Assert that normal checkout throws exception
                // for the conflict.
                Assert.Throws<CheckoutConflictException>(() => repo.Checkout(master.CanonicalName));

                // Checkout with force option should succeed.
                repo.Checkout(master.CanonicalName, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force});

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
                repo.Stage(originalFilePath);
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                // Create 2nd branch
                repo.CreateBranch("branch2");

                // Update file in main
                Touch(repo.Info.WorkingDirectory, originalFilePath, "Hello from master!\n");
                repo.Stage(originalFilePath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout branch2
                repo.Checkout("branch2");
                Touch(repo.Info.WorkingDirectory, originalFilePath, "Hello From branch2!\n");

                // Assert that checking out master throws
                // when there are unstaged commits
                Assert.Throws<CheckoutConflictException>(() => repo.Checkout("master"));

                // And when there are staged commits
                repo.Stage(originalFilePath);
                Assert.Throws<CheckoutConflictException>(() => repo.Checkout("master"));
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

                repo.Stage(relativePath);
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                // Create 2nd branch
                repo.CreateBranch("branch2");

                // Update file in main
                Touch(repo.Info.WorkingDirectory, relativePath, "Hello from master!\n");
                repo.Stage(relativePath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                // Checkout branch2
                repo.Checkout("branch2");

                // Update the context of a.txt - a.txt will then conflict between branch2 and master.
                Touch(repo.Info.WorkingDirectory, relativePath, "Hello From branch2!\n");

                // Verify that we get called for the notify conflict cb
                string conflictPath = string.Empty;

                CheckoutOptions options = new CheckoutOptions()
                {
                    OnCheckoutNotify = (path, flags) => { conflictPath = path; return false; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict,
                };

                Assert.Throws<UserCancelledException>(() => repo.Checkout("master", options));
                Assert.Equal(relativePath, conflictPath);
            }
        }

        [Fact]
        public void CheckingOutInABareRepoThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Checkout(repo.Branches["refs/heads/test"]));
                Assert.Throws<BareRepositoryException>(() => repo.Checkout("refs/heads/test"));
            }
        }

        [Fact]
        public void CheckingOutAgainstAnUnbornBranchThrows()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.True(repo.Info.IsHeadUnborn);

                Assert.Throws<UnbornBranchException>(() => repo.Checkout(repo.Head));
            }
        }

        [Fact]
        public void CheckingOutANonExistingBranchThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Checkout("i-do-not-exist"));
            }
        }

        [Fact]
        public void CheckingOutABranchWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Checkout(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(Branch)));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(string)));
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
                repo.Checkout(branch,
                    new CheckoutOptions { OnCheckoutProgress = (path, completed, total) => wasCalled = true});

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

                repo.Checkout(otherBranchName, new CheckoutOptions() { OnCheckoutProgress = (path, completed, total) => wasCalled = true});

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
                repo.Stage(relativePathUpdated);
                repo.Commit("Commit initial update file", Constants.Signature, Constants.Signature);

                // Create conflicting change
                const string relativePathConflict = "conflict.txt";
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text A");
                repo.Stage(relativePathConflict);
                repo.Commit("Initial commit of conflict.txt and update.txt", Constants.Signature, Constants.Signature);

                // Create another branch
                repo.CreateBranch("newbranch");

                // Make an edit to conflict.txt and update.txt
                Touch(repo.Info.WorkingDirectory, relativePathUpdated, "updated file text BB");
                repo.Stage(relativePathUpdated);
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text BB");
                repo.Stage(relativePathConflict);

                repo.Commit("2nd commit of conflict.txt and update.txt on master branch", Constants.Signature, Constants.Signature);

                // Checkout other branch
                repo.Checkout("newbranch");

                // Make alternate edits to conflict.txt and update.txt
                Touch(repo.Info.WorkingDirectory, relativePathUpdated, "updated file text CCC");
                repo.Stage(relativePathUpdated);
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text CCC");
                repo.Stage(relativePathConflict);
                repo.Commit("2nd commit of conflict.txt and update.txt on newbranch", Constants.Signature, Constants.Signature);

                // make conflicting change to conflict.txt
                Touch(repo.Info.WorkingDirectory, relativePathConflict, "conflict file text DDDD");
                repo.Stage(relativePathConflict);

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

                Assert.Throws<CheckoutConflictException>(() => repo.Checkout("master", options));

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
                Assert.Equal(1, repo.RetrieveStatus().Untracked.Count());
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(fullPathFileB));

                repo.Checkout(otherBranchName);

                // Verify untracked entry still exists.
                Assert.Equal(1, repo.RetrieveStatus().Untracked.Count());
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
                Assert.Equal(1, repo.RetrieveStatus().Untracked.Count());
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(fullPathFileB));

                repo.Checkout(otherBranchName, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });

                // Verify untracked entry still exists.
                Assert.Equal(1, repo.RetrieveStatus().Untracked.Count());
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
                Assert.Equal(1, repo.RetrieveStatus().Modified.Count());
                Assert.Equal(FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(fullPathFileA));

                repo.Checkout(otherBranchName);

                // Verify modified entry still exists.
                Assert.Equal(1, repo.RetrieveStatus().Modified.Count());
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
                repo.Stage(fullPathFileA);

                // Verify that there is a staged entry.
                Assert.Equal(1, repo.RetrieveStatus().Staged.Count());
                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus(fullPathFileA));

                repo.Checkout(otherBranchName);

                // Verify staged entry still exists.
                Assert.Equal(1, repo.RetrieveStatus().Staged.Count());
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

                Assert.Equal(1, repo.RetrieveStatus().Ignored.Count());

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(ignoredFilePath));

                repo.Checkout(otherBranchName);

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

                Assert.Equal(1, repo.RetrieveStatus().Ignored.Count());

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(ignoredFilePath));

                repo.Checkout(otherBranchName, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });

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
                repo.Stage(fullPath);
                repo.Commit("2nd commit", Constants.Signature, Constants.Signature);

                Assert.False(repo.Info.IsHeadDetached);

                repo.Checkout(initial);

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

                repo.Checkout(remoteBranchSpec);

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

                repo.Checkout("refs/heads/logo", new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });

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
        [InlineData("refs/tags/lw")]
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

                Branch initialHead = repo.Checkout("6dcf9bf");

                repo.Checkout(commitPointer);

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
            using (var repo = new Repository(path, new RepositoryOptions{ Identity = Constants.Identity }))
            {
                // Set the working directory to the current head
                ResetAndCleanWorkingDirectory(repo);
                Assert.False(repo.RetrieveStatus().IsDirty);

                Branch initialHead = repo.Checkout("6dcf9bf");

                Assert.True(repo.Info.IsHeadDetached);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newHead = repo.Checkout(repo.Branches["master"]);

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

                repo.Checkout("6dcf9bf");
                Assert.True(repo.Info.IsHeadDetached);

                var branch = repo.Checkout(shortBranchName);

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

                Branch previousHead = repo.Checkout("i-do-numbers");
                repo.Checkout("diff-test-cases");

                //Go back to previous branch checked out
                var branch = repo.Checkout(@"@{-1}");

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
                repo.Checkout(master);

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                // Checkout in detached mode
                repo.Checkout(master.Tip.Sha);

                Assert.True(repo.Info.IsHeadDetached);
                AssertRefLogEntry(repo, "HEAD",
                    string.Format("checkout: moving from master to {0}", master.Tip.Sha),
                    master.Tip.Id, master.Tip.Id, Constants.Identity, before);

                // Checkout detached "HEAD" => nothing should happen
                reflogEntriesCount = repo.Refs.Log(repo.Refs.Head).Count();

                repo.Checkout(repo.Head);

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());

                // Checkout attached "HEAD" => nothing should happen
                repo.Checkout("master");
                reflogEntriesCount = repo.Refs.Log(repo.Refs.Head).Count();

                repo.Checkout(repo.Head);

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());

                repo.Checkout("HEAD");

                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());
            }
        }

        [Fact]
        public void CheckoutLowerCasedHeadThrows()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Checkout("head"));
            }
        }

        [Fact]
        public void CanCheckoutAttachedHead()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.False(repo.Info.IsHeadDetached);

                repo.Checkout(repo.Head);
                Assert.False(repo.Info.IsHeadDetached);

                repo.Checkout("HEAD");
                Assert.False(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void CanCheckoutDetachedHead()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Checkout(repo.Head.Tip.Sha);

                Assert.True(repo.Info.IsHeadDetached);

                repo.Checkout(repo.Head);
                Assert.True(repo.Info.IsHeadDetached);

                repo.Checkout("HEAD");
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

                repo.Checkout(originalBranch);
                Assert.False(repo.RetrieveStatus().IsDirty);

                repo.CheckoutPaths(checkoutFrom, new[] { path });

                Assert.Equal(expectedStatus, repo.RetrieveStatus(path));
                Assert.Equal(1, repo.RetrieveStatus().Count());
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
                Assert.Throws(typeof(ArgumentNullException),
                    () => repo.CheckoutPaths("i-do-numbers", null));

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

        /// <summary>
        /// Helper method to populate a simple repository with
        /// a single file and two branches.
        /// </summary>
        /// <param name="repo">Repository to populate</param>
        private void PopulateBasicRepository(IRepository repo)
        {
            // Generate a .gitignore file.
            string gitIgnoreFilePath = Touch(repo.Info.WorkingDirectory, ".gitignore", "bin");
            repo.Stage(gitIgnoreFilePath);

            string fullPathFileA = Touch(repo.Info.WorkingDirectory, originalFilePath, originalFileContent);
            repo.Stage(fullPathFileA);

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
