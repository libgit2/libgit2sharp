using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections.Generic;
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
        public void CanGetRepositoryForWorktree()
        {
            var testpath = SandboxWorktreeTestRepo();
            var repoPath = testpath;
            using (var repo = new Repository(repoPath))
            {
                var worktree = repo.Worktrees["logo"];

                Assert.Equal("logo", worktree.Name);
                var worktreeRepo = worktree.WorktreeRepository;
                Assert.NotNull(worktreeRepo);
            }
        }
    }
}
