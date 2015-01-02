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
                Assert.Equal(1, repo.RetrieveStatus().Untracked.Count());

                repo.RemoveUntrackedFiles();

                // Verify that there are the expected number of entries and 0 untracked files
                Assert.Equal(5, repo.RetrieveStatus().Count());
                Assert.Equal(0, repo.RetrieveStatus().Untracked.Count());
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
