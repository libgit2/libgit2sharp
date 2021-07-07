using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void CanAddWorktree()
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
            }
        }

        [Fact]
        public void CanAddLockedWorktree()
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
            }
        }

        [Fact]
        public void CanReadRemotesConfigFromWorktree()
        {
            const string worktreeName = "i-do-numbers";
            var expectedRemotes = new[] { "no_url", "origin" };

            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                var worktree = repo.Worktrees[worktreeName];
                using (var worktreeRepo = worktree.WorktreeRepository)
                {
                    var remoteNames = worktreeRepo.Network.Remotes.Select(x => x.Name).OrderBy(x => x).ToArray();
                    Assert.Equal(expectedRemotes, remoteNames);
                }
            }
        }

        /// <summary>
        /// Regression test for the https://github.com/libgit2/libgit2sharp/issues/1678.
        /// </summary>
        [Fact]
        public void CanReadRemotesConfigFromWorktreeAfterConfigAccess()
        {
            const string worktreeName = "i-do-numbers";
            var expectedRemotes = new[] { "no_url", "origin" };

            var repoPath = SandboxWorktreeTestRepo();
            using (var repo = new Repository(repoPath))
            {
                var worktree = repo.Worktrees[worktreeName];
                using (var worktreeRepo = worktree.WorktreeRepository)
                {
                    _ = repo.Head.RemoteName;
                    // Extra dummy access to Configuration. Previously it affected internal repository configuration state.
                    worktreeRepo.Config.HasConfig(ConfigurationLevel.Local);

                    var remoteNames = worktreeRepo.Network.Remotes.Select(x => x.Name).OrderBy(x => x).ToArray();
                    Assert.Equal(expectedRemotes, remoteNames);
                }
            }
        }
    }
}
