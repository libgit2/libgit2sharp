using LibGit2Sharp.Interactive;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class InteractiveFixture : BaseFixture
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InteractiveStateHasExpectedValuesForNewRepo(bool isBare)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.DirectoryPath, isBare))
            {
                var state = repo.InteractiveState;
                Assert.Equal("master", state.HeadName);
                Assert.Equal(Operation.None, state.PendingOperation);
            }
        }

        [Fact]
        public void InteractiveStateHasExpectedValuesForABareRepo()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var state = repo.InteractiveState;
                Assert.Equal("master", state.HeadName);
                Assert.Equal(Operation.None, state.PendingOperation);
            }
        }

        [Fact]
        public void InteractiveStateHasExpectedValuesForStandardRepo()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var state = repo.InteractiveState;
                Assert.Equal("master", state.HeadName);
                Assert.Equal(Operation.None, state.PendingOperation);

                repo.Checkout("track-local");
                Assert.Equal("track-local", state.HeadName);
            }
        }

        [Fact]
        public void InteractiveStateHasExpectedValuesForDetachedHead()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Checkout(repo.Head.Tip.Sha);

                var state = repo.InteractiveState;
                Assert.Equal("(32eab9c...)", state.HeadName);
                Assert.Equal(Operation.None, state.PendingOperation);
            }
        }

        [Fact]
        public void InteractiveStateHasExpectedValuesForInteractiveRebase()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            path.Touch("rebase-merge", "interactive");
            path.Touch("rebase-merge", "head-name", "refs/heads/master");

            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Checkout(repo.Head.Tip.Sha);

                var state = repo.InteractiveState;
                Assert.Equal("master", state.HeadName);
                Assert.Equal(Operation.RebaseInteractive, state.PendingOperation);
            }
        }
    }
}
