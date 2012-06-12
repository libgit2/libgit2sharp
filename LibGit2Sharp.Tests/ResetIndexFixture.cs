using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ResetIndexFixture : BaseFixture
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResetANewlyInitializedRepositoryThrows(bool isBare)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (Repository repo = Repository.Init(scd.DirectoryPath, isBare))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Reset());
            }
        }

        [Fact]
        public void ResettingInABareRepositoryThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Reset());
            }
        }

        private bool IsStaged(StatusEntry entry)
        {
            if ((entry.State & FileStatus.Added) == FileStatus.Added)
            {
                return true;
            }

            if ((entry.State & FileStatus.Staged) == FileStatus.Staged)
            {
                return true;
            }

            if ((entry.State & FileStatus.Removed) == FileStatus.Removed)
            {
                return true;
            }

            return false;
        }

        [Fact]
        public void ResetTheIndexWithTheHeadUnstagesEverything()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(path.DirectoryPath))
            {
                RepositoryStatus oldStatus = repo.Index.RetrieveStatus();
                Assert.Equal(3, oldStatus.Where(IsStaged).Count());

                repo.Reset();

                RepositoryStatus newStatus = repo.Index.RetrieveStatus();
                Assert.Equal(0, newStatus.Where(IsStaged).Count());
            }
        }

        [Fact]
        public void CanResetTheIndexToTheContentOfACommit()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(path.DirectoryPath))
            {
                repo.Reset("be3563a");

                RepositoryStatus newStatus = repo.Index.RetrieveStatus();

                var expected = new[] { "1.txt", "1" + Path.DirectorySeparatorChar + "branch_file.txt", "deleted_staged_file.txt", 
                    "deleted_unstaged_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt" };

                Assert.Equal(expected.Length, newStatus.Where(IsStaged).Count());
                Assert.Equal(expected, newStatus.Removed);
            }
        }

        [Fact]
        public void CanResetTheIndexToASubsetOfTheContentOfACommit()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(path.DirectoryPath))
            {
                repo.Reset("5b5b025", new[]{ "new.txt" });

                Assert.Equal("a8233120f6ad708f843d861ce2b7228ec4e3dec6", repo.Index["README"].Id.Sha);
                Assert.Equal("fa49b077972391ad58037050f2a75f74e3671e92", repo.Index["new.txt"].Id.Sha);
            }
        }
    }
}
