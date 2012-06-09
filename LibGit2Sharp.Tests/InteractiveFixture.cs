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
                Assert.Equal("master", repo.Head.Name);
                Assert.Equal(PendingOperation.None, repo.Info.PendingOperation);
            }
        }

        [Fact]
        public void InteractiveStateHasExpectedValuesForABareRepo()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal("master", repo.Head.Name);
                Assert.Equal(PendingOperation.None, repo.Info.PendingOperation);
            }
        }

        [Fact]
        public void InteractiveStateHasExpectedValuesForStandardRepo()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Equal("master", repo.Head.Name);
                Assert.Equal(PendingOperation.None, repo.Info.PendingOperation);

                repo.Checkout("track-local");
                Assert.Equal("track-local", repo.Head.Name);
            }
        }

        [Fact]
        public void InteractiveStateHasExpectedValuesForDetachedHead()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Checkout(repo.Head.Tip.Sha);

                Assert.Equal("(32eab9c...)", repo.Head.Name);
                Assert.Equal(PendingOperation.None, repo.Info.PendingOperation);
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

                Assert.Equal("master", repo.Head.Name);
                Assert.Equal(PendingOperation.RebaseInteractive, repo.Info.PendingOperation);
            }
        }
    }
}
