using System.IO;
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
                Assert.Throws<BareRepositoryException>(() => repo.Reset());
            }
        }

        [Fact]
        public void ResetANewlyInitializedNonBareRepositoryThrows()
        {
            string repoPath = InitNewRepository(false);

            using (var repo = new Repository(repoPath))
            {
                Assert.Throws<UnbornBranchException>(() => repo.Reset());
            }
        }

        [Fact]
        public void ResettingInABareRepositoryThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Reset());
            }
        }

        private static bool IsStaged(StatusEntry entry)
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Equal(3, oldStatus.Where(IsStaged).Count());

                var reflogEntriesCount = repo.Refs.Log(repo.Refs.Head).Count();

                repo.Reset();

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(0, newStatus.Where(IsStaged).Count());

                // Assert that no reflog entry is created
                Assert.Equal(reflogEntriesCount, repo.Refs.Log(repo.Refs.Head).Count());
            }
        }

        [Fact]
        public void CanResetTheIndexToTheContentOfACommitWithCommittishAsArgument()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset("be3563a");

                RepositoryStatus newStatus = repo.RetrieveStatus();

                var expected = new[] { "1.txt", Path.Combine("1", "branch_file.txt"), "deleted_staged_file.txt",
                    "deleted_unstaged_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt" };

                Assert.Equal(expected.Length, newStatus.Where(IsStaged).Count());
                Assert.Equal(expected, newStatus.Removed.Select(s => s.FilePath));
            }
        }

        [Fact]
        public void CanResetTheIndexToTheContentOfACommitWithCommitAsArgument()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(repo.Lookup<Commit>("be3563a"));

                RepositoryStatus newStatus = repo.RetrieveStatus();

                var expected = new[] { "1.txt", Path.Combine("1", "branch_file.txt"), "deleted_staged_file.txt",
                    "deleted_unstaged_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt" };

                Assert.Equal(expected.Length, newStatus.Where(IsStaged).Count());
                Assert.Equal(expected, newStatus.Removed.Select(s => s.FilePath));
            }
        }

        [Fact]
        public void CanResetTheIndexToASubsetOfTheContentOfACommitWithCommittishAsArgument()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset("5b5b025", new[]{ "new.txt" });

                Assert.Equal("a8233120f6ad708f843d861ce2b7228ec4e3dec6", repo.Index["README"].Id.Sha);
                Assert.Equal("fa49b077972391ad58037050f2a75f74e3671e92", repo.Index["new.txt"].Id.Sha);
            }
        }

        [Fact]
        public void CanResetTheIndexToASubsetOfTheContentOfACommitWithCommitAsArgumentAndLaxUnmatchedExplicitPathsValidation()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(repo.Lookup<Commit>("5b5b025"), new[] { "new.txt", "non-existent-path-28.txt" },
                    new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false });

                Assert.Equal("a8233120f6ad708f843d861ce2b7228ec4e3dec6", repo.Index["README"].Id.Sha);
                Assert.Equal("fa49b077972391ad58037050f2a75f74e3671e92", repo.Index["new.txt"].Id.Sha);
            }
        }

        [Fact]
        public void ResettingTheIndexToASubsetOfTheContentOfACommitWithCommitAsArgumentAndStrictUnmatchedPathspecsValidationThrows()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                Assert.Throws<UnmatchedPathException>(() =>
                    repo.Reset(repo.Lookup<Commit>("5b5b025"), new[] { "new.txt", "non-existent-path-28.txt" }, new ExplicitPathsOptions()));
            }
        }

        [Fact]
        public void CanResetTheIndexWhenARenameExists()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Move("branch_file.txt", "renamed_branch_file.txt");
                repo.Reset(repo.Lookup<Commit>("32eab9c"));

                RepositoryStatus status = repo.RetrieveStatus();
                Assert.Equal(0, status.Where(IsStaged).Count());
            }
        }

        [Fact]
        public void CanResetSourceOfARenameInIndex()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Move("branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Equal(1, oldStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.Nonexistent, oldStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                repo.Reset(repo.Lookup<Commit>("32eab9c"), new string[] { "branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(0, newStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.Missing, newStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.Added, newStatus["renamed_branch_file.txt"].State);
            }
        }

        [Fact]
        public void CanResetTargetOfARenameInIndex()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Move("branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Equal(1, oldStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                repo.Reset(repo.Lookup<Commit>("32eab9c"), new string[] { "renamed_branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(0, newStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.Untracked, newStatus["renamed_branch_file.txt"].State);
                Assert.Equal(FileStatus.Removed, newStatus["branch_file.txt"].State);
            }
        }
    }
}
