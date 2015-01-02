using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CurrentOperationFixture : BaseFixture
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentOperationIsNoneForNewRepo(bool isBare)
        {
            string repoPath = InitNewRepository(isBare);

            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);
            }
        }

        [Fact]
        public void CurrentOperationInNoneForABareRepo()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);
            }
        }

        [Theory]
        [InlineData("MERGE_HEAD", CurrentOperation.Merge)]
        [InlineData("REVERT_HEAD", CurrentOperation.Revert)]
        [InlineData("CHERRY_PICK_HEAD", CurrentOperation.CherryPick)]
        [InlineData("BISECT_LOG", CurrentOperation.Bisect)]
        [InlineData("rebase-apply/rebasing", CurrentOperation.Rebase)]
        [InlineData("rebase-apply/applying", CurrentOperation.ApplyMailbox)]
        [InlineData("rebase-apply/whatever", CurrentOperation.ApplyMailboxOrRebase)]
        [InlineData("rebase-merge/interactive", CurrentOperation.RebaseInteractive)]
        [InlineData("rebase-merge/whatever", CurrentOperation.RebaseMerge)]
        public void CurrentOperationHasExpectedPendingOperationValues(string stateFile, CurrentOperation expectedState)
        {
            string path = SandboxStandardTestRepo();

            Touch(Path.Combine(path, ".git"), stateFile);

            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedState, repo.Info.CurrentOperation);
            }
        }
    }
}
