using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ResetIndexFixture : BaseFixture
    {
        [Fact]
        public void ResetANewlyInitializedBareRepositoryThrows()
        {
            string repoPath = InitNewRepository(true);

            using (var repo = new Repository(repoPath))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Index.Replace(repo.Head.Tip));
            }
        }

        [Fact]
        public void ResettingInABareRepositoryThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Index.Replace(repo.Head.Tip));
            }
        }

        private static bool IsStaged(StatusEntry entry)
        {
            if ((entry.State & FileStatus.NewInIndex) == FileStatus.NewInIndex)
            {
                return true;
            }

            if ((entry.State & FileStatus.ModifiedInIndex) == FileStatus.ModifiedInIndex)
            {
                return true;
            }

            if ((entry.State & FileStatus.DeletedFromIndex) == FileStatus.DeletedFromIndex)
            {
                return true;
            }

            return false;
        }

        [Fact]
        public void ResetTheIndexWithTheHeadUnstagesEverything()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Equal(3, oldStatus.Where(IsStaged).Count());

                var reflogEntriesCount = repo.Refs.Log(repo.Refs.Head).Count();

                repo.Index.Replace(repo.Head.Tip);

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.DoesNotContain(newStatus, IsStaged);

                // Assert that no reflog entry is created
                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());
            }
        }

        [Fact]
        public void CanResetTheIndexToTheContentOfACommitWithCommitAsArgument()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Index.Replace(repo.Lookup<Commit>("be3563a"));

                RepositoryStatus newStatus = repo.RetrieveStatus();

                var expected = new[] { "1.txt", string.Join("/", "1", "branch_file.txt"), "deleted_staged_file.txt",
                    "deleted_unstaged_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt" };

                Assert.Equal(expected.Length, newStatus.Where(IsStaged).Count());
                Assert.Equal(expected, newStatus.Removed.Select(s => s.FilePath));
            }
        }

        [Fact]
        public void CanResetTheIndexToASubsetOfTheContentOfACommitWithCommitAsArgumentAndLaxUnmatchedExplicitPathsValidation()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Index.Replace(repo.Lookup<Commit>("5b5b025"), new[] { "new.txt", "non-existent-path-28.txt" },
                    new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false });

                Assert.Equal("a8233120f6ad708f843d861ce2b7228ec4e3dec6", repo.Index["README"].Id.Sha);
                Assert.Equal("fa49b077972391ad58037050f2a75f74e3671e92", repo.Index["new.txt"].Id.Sha);
            }
        }

        [Fact]
        public void ResettingTheIndexToASubsetOfTheContentOfACommitWithCommitAsArgumentAndStrictUnmatchedPathspecsValidationThrows()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Assert.Throws<UnmatchedPathException>(() =>
                    repo.Index.Replace(repo.Lookup<Commit>("5b5b025"), new[] { "new.txt", "non-existent-path-28.txt" }, new ExplicitPathsOptions()));
            }
        }

        [Fact]
        public void CanResetTheIndexWhenARenameExists()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Commands.Move(repo, "branch_file.txt", "renamed_branch_file.txt");
                repo.Index.Replace(repo.Lookup<Commit>("32eab9c"));

                RepositoryStatus status = repo.RetrieveStatus();
                Assert.DoesNotContain(status, IsStaged);
            }
        }

        [Fact]
        public void CanResetSourceOfARenameInIndex()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Commands.Move(repo, "branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Single(oldStatus.RenamedInIndex);
                Assert.Equal(FileStatus.Nonexistent, oldStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                repo.Index.Replace(repo.Lookup<Commit>("32eab9c"), new string[] { "branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Empty(newStatus.RenamedInIndex);
                Assert.Equal(FileStatus.DeletedFromWorkdir, newStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.NewInIndex, newStatus["renamed_branch_file.txt"].State);
            }
        }

        [Fact]
        public void CanResetTargetOfARenameInIndex()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Commands.Move(repo, "branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Single(oldStatus.RenamedInIndex);
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                repo.Index.Replace(repo.Lookup<Commit>("32eab9c"), new string[] { "renamed_branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Empty(newStatus.RenamedInIndex);
                Assert.Equal(FileStatus.NewInWorkdir, newStatus["renamed_branch_file.txt"].State);
                Assert.Equal(FileStatus.DeletedFromIndex, newStatus["branch_file.txt"].State);
            }
        }
    }
}
