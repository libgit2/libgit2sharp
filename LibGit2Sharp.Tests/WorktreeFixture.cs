using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class WorktreeFixture : BaseFixture
    {
        [Fact]
        public void RetrievingWorktreeForRandomNameReturnsNull()
        {
            var path = SandboxWorktreeTestRepo();
            using (var repo = new Repository(path))
            {
                var worktree = repo.Worktrees["random"];
                Assert.Null(worktree);
            }
        }

        [Fact]
        public void RetrievingWorktreeForWorktreeNameReturnsWorktree()
        {
            var path = SandboxWorktreeTestRepo();
            using (var repo = new Repository(path))
            {
                var worktree = repo.Worktrees["logo"];
                Assert.NotNull(worktree);
            }
        }

        [Fact]
        public void CanEnumerateRepositoryWorktrees()
        {
            var expectedWorktrees = new[]
            {
                "i-do-numbers",
                "logo",
            };

            var path = SandboxWorktreeTestRepo();
            using (var repo = new Repository(path))
            {
                var worktrees = repo.Worktrees.OrderBy(w => w.Name, StringComparer.Ordinal);

                Assert.Equal(expectedWorktrees, worktrees.Select(w => w.Name).ToArray());
            }
        }

        [Fact]
        public void CanViewLockStatusForWorktrees()
        {
            var testpath = SandboxWorktreeTestRepo();
            var repoPath = testpath;
            using (var repo = new Repository(repoPath))
            {
                // locked
                var worktreeLogo = repo.Worktrees["logo"];
                Assert.Equal("logo", worktreeLogo.Name);
                Assert.True(worktreeLogo.IsLocked);
                Assert.Equal("Test lock reason\n", worktreeLogo.LockReason);

                // not locked
                var worktreeIDoNumbers = repo.Worktrees["i-do-numbers"];
                Assert.Equal("i-do-numbers", worktreeIDoNumbers.Name);
                Assert.False(worktreeIDoNumbers.IsLocked);
                Assert.Null(worktreeIDoNumbers.LockReason);
            }
        }

        [Fact]
        public void CanUnlockWorktree()
        {
            var testpath = SandboxWorktreeTestRepo();
            var repoPath = testpath;
            using (var repo = new Repository(repoPath))
            {
                // locked
                var worktreeLocked = repo.Worktrees["logo"];
                Assert.Equal("logo", worktreeLocked.Name);
                Assert.True(worktreeLocked.IsLocked);
                Assert.Equal("Test lock reason\n", worktreeLocked.LockReason);

                worktreeLocked.Unlock();

                // unlocked
                var worktreeUnlocked = repo.Worktrees["logo"];
                Assert.Equal("logo", worktreeLocked.Name);
                Assert.False(worktreeUnlocked.IsLocked);
                Assert.Null(worktreeUnlocked.LockReason);
            }
        }

        [Fact]
        public void CanLockWorktree()
        {
            var testpath = SandboxWorktreeTestRepo();
            var repoPath = testpath;
            using (var repo = new Repository(repoPath))
            {
                // unlocked
                var worktreeUnlocked = repo.Worktrees["i-do-numbers"];
                Assert.Equal("i-do-numbers", worktreeUnlocked.Name);
                Assert.False(worktreeUnlocked.IsLocked);
                Assert.Null(worktreeUnlocked.LockReason);

                worktreeUnlocked.Lock("add a lock");

                // locked
                var worktreeLocked = repo.Worktrees["i-do-numbers"];
                Assert.Equal("i-do-numbers", worktreeLocked.Name);
                Assert.True(worktreeLocked.IsLocked);
                Assert.Equal("add a lock", worktreeLocked.LockReason);
            }
        }

        [Fact]
        public void CanGetRepositoryForWorktree()
        {
            var testpath = SandboxWorktreeTestRepo();
            var repoPath = testpath;
            using (var repo = new Repository(repoPath))
            {
                var worktree = repo.Worktrees["logo"];

                Assert.Equal("logo", worktree.Name);
                using (var repository = worktree.WorktreeRepository)
                {
                    Assert.NotNull(repository);
                }
            }
        }

        [Fact]
        public void CanPruneUnlockedWorktree()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());

                // unlocked
                var worktreeUnlocked = repo.Worktrees["i-do-numbers"];
                Assert.Equal("i-do-numbers", worktreeUnlocked.Name);
                Assert.False(worktreeUnlocked.IsLocked);

                Assert.True(repo.Worktrees.Prune(worktreeUnlocked));

                Assert.Single(repo.Worktrees);
            }
        }

        [Fact]
        public void CanPruneDeletedWorktree()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());
                var repoPath2 = repo.Info.Path;
                var repoWd = repo.Info.WorkingDirectory;
                // unlocked
                var worktreeUnlocked = repo.Worktrees["i-do-numbers"];
                Assert.Equal("i-do-numbers", worktreeUnlocked.Name);
                Assert.False(worktreeUnlocked.IsLocked);
                using (var wtRepo = worktreeUnlocked.WorktreeRepository)
                {
                    var info = wtRepo.Info;

                    Directory.Delete(info.WorkingDirectory, true);
                }

                Assert.True(repo.Worktrees.Prune(worktreeUnlocked));

                Assert.Single(repo.Worktrees);
            }
        }

        [Fact]
        public void CanNotPruneLockedWorktree()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());

                // locked
                var worktreeUnlocked = repo.Worktrees["logo"];
                Assert.Equal("logo", worktreeUnlocked.Name);
                Assert.True(worktreeUnlocked.IsLocked);

                Assert.Throws<LibGit2SharpException>(() => repo.Worktrees.Prune(worktreeUnlocked));
            }
        }

        [Fact]
        public void CanUnlockThenPruneLockedWorktree()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());

                // locked
                var worktreeLocked = repo.Worktrees["logo"];
                Assert.Equal("logo", worktreeLocked.Name);
                Assert.True(worktreeLocked.IsLocked);

                worktreeLocked.Unlock();

                repo.Worktrees.Prune(worktreeLocked);

                Assert.Single(repo.Worktrees);
            }
        }

        [Fact]
        public void CanForcePruneLockedWorktree()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());

                // locked
                var worktreeLocked = repo.Worktrees["logo"];
                Assert.Equal("logo", worktreeLocked.Name);
                Assert.True(worktreeLocked.IsLocked);

                repo.Worktrees.Prune(worktreeLocked, true);

                Assert.Single(repo.Worktrees);
            }
        }

        [Fact]
        public void CanAddWorktree_WithUncommitedChanges()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());

                var name = "blah";
                var path = Path.Combine(repo.Info.WorkingDirectory, "..", "worktrees", name);
                var worktree = repo.Worktrees.Add(name, path, false);
                Assert.Equal(name, worktree.Name);
                Assert.False(worktree.IsLocked);

                Assert.Equal(3, repo.Worktrees.Count());

                // Check that branch contains same number of files and folders
                Assert.True(repo.RetrieveStatus().IsDirty);
                var filesInMain = GetFilesOfRepo(repoPath);
                var filesInBranch = GetFilesOfRepo(path);
                Assert.NotEqual(filesInMain, filesInBranch);

                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                Assert.False(repo.RetrieveStatus().IsDirty);
                filesInMain = GetFilesOfRepo(repoPath);
                filesInBranch = GetFilesOfRepo(path);
                Assert.Equal(filesInMain, filesInBranch);
            }
        }

        [Fact]
        public void CanAddWorktree_WithCommitedChanges()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                // stage all changes
                Commands.Stage(repo, "*");
                repo.Commit("Apply all changes", Constants.Signature, Constants.Signature);

                Assert.Equal(2, repo.Worktrees.Count());

                var name = "blah";
                var path = Path.Combine(repo.Info.WorkingDirectory, "..", "worktrees", name);
                var worktree = repo.Worktrees.Add(name, path, false);
                Assert.Equal(name, worktree.Name);
                Assert.False(worktree.IsLocked);

                Assert.Equal(3, repo.Worktrees.Count());

                // Check that branch contains same number of files and folders
                Assert.False(repo.RetrieveStatus().IsDirty);
                var filesInMain = GetFilesOfRepo(repoPath);
                var filesInBranch = GetFilesOfRepo(path);

                Assert.Equal(filesInMain, filesInBranch);
            }
        }

        [Fact]
        public void CanAddLockedWorktree_WithUncommitedChanges()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());

                var name = "blah";
                var path = Path.Combine(repo.Info.WorkingDirectory, "..", "worktrees", name);
                var worktree = repo.Worktrees.Add(name, path, true);
                Assert.Equal(name, worktree.Name);
                Assert.True(worktree.IsLocked);

                Assert.Equal(3, repo.Worktrees.Count());

                // Check that branch contains same number of files and folders
                Assert.True(repo.RetrieveStatus().IsDirty);
                var filesInMain = GetFilesOfRepo(repoPath);
                var filesInBranch = GetFilesOfRepo(path);
                Assert.NotEqual(filesInMain, filesInBranch);

                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                Assert.False(repo.RetrieveStatus().IsDirty);
                filesInMain = GetFilesOfRepo(repoPath);
                filesInBranch = GetFilesOfRepo(path);
                Assert.Equal(filesInMain, filesInBranch);
            }
        }

        [Fact]
        public void CanAddLockedWorktree_WithCommitedChanges()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                // stage all changes
                Commands.Stage(repo, "*");
                repo.Commit("Apply all changes", Constants.Signature, Constants.Signature);

                Assert.Equal(2, repo.Worktrees.Count());

                var name = "blah";
                var path = Path.Combine(repo.Info.WorkingDirectory, "..", "worktrees", name);
                var worktree = repo.Worktrees.Add(name, path, true);
                Assert.Equal(name, worktree.Name);
                Assert.True(worktree.IsLocked);

                Assert.Equal(3, repo.Worktrees.Count());

                // Check that branch contains same number of files and folders
                Assert.False(repo.RetrieveStatus().IsDirty);
                var filesInMain = GetFilesOfRepo(repoPath);
                var filesInBranch = GetFilesOfRepo(path);
                Assert.Equal(filesInMain, filesInBranch);
            }
        }

        [Fact]
        public void CanAddWorktreeForCommittish()
        {
            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(2, repo.Worktrees.Count());

                var name = "blah";
                var committish = "diff-test-cases";
                var path = Path.Combine(repo.Info.WorkingDirectory, "..", "worktrees", name);
                var worktree = repo.Worktrees.Add(committish, name, path, false);
                Assert.Equal(name, worktree.Name);
                Assert.False(worktree.IsLocked);
                using (var repository = worktree.WorktreeRepository)
                {
                    Assert.Equal(committish, repository.Head.FriendlyName);
                }
                Assert.Equal(3, repo.Worktrees.Count());

                // Check that branch contains same number of files and folders
                var filesInCommittish = new string[] { "numbers.txt", "super-file.txt" };
                var filesInBranch = GetFilesOfRepo(path);
                Assert.Equal(filesInCommittish, filesInBranch);
            }
        }

        private static IEnumerable<string> GetFilesOfRepo(string repoPath)
        {
            return Directory.GetFiles(repoPath, "*", SearchOption.AllDirectories)
                .Where(fileName => !fileName.StartsWith(Path.Combine(repoPath, ".git")))
                .Select(fileName => fileName.Replace($"{repoPath}{Path.DirectorySeparatorChar}", ""))
                .OrderBy(fileName => fileName)
                .ToList();
        }
    }
}
