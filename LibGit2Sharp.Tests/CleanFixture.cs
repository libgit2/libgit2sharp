using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class CleanFixture : BaseFixture
    {
        [Fact]
        public void CanCleanWorkingDirectory()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                // Verify that there are the expected number of entries and untracked files
                Assert.Equal(6, repo.Index.RetrieveStatus().Count());
                Assert.Equal(1, repo.Index.RetrieveStatus().Untracked.Count());

                repo.RemoveUntrackedFiles();

                // Verify that there are the expected number of entries and 0 untracked files
                Assert.Equal(5, repo.Index.RetrieveStatus().Count());
                Assert.Equal(0, repo.Index.RetrieveStatus().Untracked.Count());
            }
        }

        [Fact]
        public void CannotCleanABareRepository()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<BareRepositoryException>(() => repo.RemoveUntrackedFiles());
            }
        }
    }
}
