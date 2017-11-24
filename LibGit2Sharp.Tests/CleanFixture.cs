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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Verify that there are the expected number of entries and untracked files
                Assert.Equal(6, repo.RetrieveStatus().Count());
                Assert.Single(repo.RetrieveStatus().Untracked);

                repo.RemoveUntrackedFiles();

                // Verify that there are the expected number of entries and 0 untracked files
                Assert.Equal(5, repo.RetrieveStatus().Count());
                Assert.Empty(repo.RetrieveStatus().Untracked);
            }
        }

        [Fact]
        public void CannotCleanABareRepository()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<BareRepositoryException>(() => repo.RemoveUntrackedFiles());
            }
        }
    }
}
