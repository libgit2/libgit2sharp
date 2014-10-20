using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class UnstageFixture : BaseFixture
    {

        [Fact]
        public void StagingANewVersionOfAFileThenUnstagingItRevertsTheBlobToTheVersionOfHead()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string filename = Path.Combine("1", "branch_file.txt");
                const string posixifiedFileName = "1/branch_file.txt";
                ObjectId blobId = repo.Index[posixifiedFileName].Id;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                File.AppendAllText(fullpath, "Is there there anybody out there?");
                repo.Stage(filename);

                Assert.Equal(count, repo.Index.Count);
                Assert.NotEqual((blobId), repo.Index[posixifiedFileName].Id);

                repo.Unstage(posixifiedFileName);

                Assert.Equal(count, repo.Index.Count);
                Assert.Equal(blobId, repo.Index[posixifiedFileName].Id);
            }
        }

        [Fact]
        public void CanStageAndUnstageAnIgnoredFile()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, ".gitignore", "*.ign" + Environment.NewLine);

                const string relativePath = "Champa.ign";
                Touch(repo.Info.WorkingDirectory, relativePath, "On stage!" + Environment.NewLine);

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(relativePath));

                repo.Stage(relativePath, new StageOptions { IncludeIgnored = true });
                Assert.Equal(FileStatus.Added, repo.RetrieveStatus(relativePath));

                repo.Unstage(relativePath);
                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [InlineData("deleted_unstaged_file.txt", FileStatus.Missing, true, FileStatus.Missing, true, 0)]
        [InlineData("modified_unstaged_file.txt", FileStatus.Modified, true, FileStatus.Modified, true, 0)]
        [InlineData("modified_staged_file.txt", FileStatus.Staged, true, FileStatus.Modified, true, 0)]
        [InlineData("new_tracked_file.txt", FileStatus.Added, true, FileStatus.Untracked, false, -1)]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed, false, FileStatus.Missing, true, 1)]
        public void CanUnstage(
            string relativePath, FileStatus currentStatus, bool doesCurrentlyExistInTheIndex,
            FileStatus expectedStatusOnceStaged, bool doesExistInTheIndexOnceStaged, int expectedIndexCountVariation)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;
                Assert.Equal(doesCurrentlyExistInTheIndex, (repo.Index[relativePath] != null));
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                repo.Unstage(relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
                Assert.Equal(doesExistInTheIndexOnceStaged, (repo.Index[relativePath] != null));
                Assert.Equal(expectedStatusOnceStaged, repo.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void UnstagingUnknownPathsWithStrictUnmatchedExplicitPathsValidationThrows(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Assert.Throws<UnmatchedPathException>(() => repo.Unstage(relativePath, new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void CanUnstageUnknownPathsWithLaxUnmatchedExplicitPathsValidation(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Assert.DoesNotThrow(() => repo.Unstage(relativePath, new ExplicitPathsOptions() { ShouldFailOnUnmatchedPath = false }));
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void CanUnstageTheRemovalOfAFile()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                const string filename = "deleted_staged_file.txt";

                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                Assert.False(File.Exists(fullPath));

                Assert.Equal(FileStatus.Removed, repo.RetrieveStatus(filename));

                repo.Unstage(filename);
                Assert.Equal(count + 1, repo.Index.Count);

                Assert.Equal(FileStatus.Missing, repo.RetrieveStatus(filename));
            }
        }

        [Fact]
        public void CanUnstageUntrackedFileAgainstAnOrphanedHead()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                const string relativePath = "a.txt";
                Touch(repo.Info.WorkingDirectory, relativePath, "hello test file\n");

                repo.Stage(relativePath);

                repo.Unstage(relativePath);
                RepositoryStatus status = repo.RetrieveStatus();
                Assert.Equal(0, status.Staged.Count());
                Assert.Equal(1, status.Untracked.Count());

                Assert.Throws<UnmatchedPathException>(() => repo.Unstage("i-dont-exist", new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void UnstagingUnknownPathsAgainstAnOrphanedHeadWithStrictUnmatchedExplicitPathsValidationThrows(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/orphaned");
                Assert.True(repo.Info.IsHeadUnborn);

                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Assert.Throws<UnmatchedPathException>(() => repo.Unstage(relativePath, new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void CanUnstageUnknownPathsAgainstAnOrphanedHeadWithLaxUnmatchedExplicitPathsValidation(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/orphaned");
                Assert.True(repo.Info.IsHeadUnborn);

                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Assert.DoesNotThrow(() => repo.Unstage(relativePath));
                Assert.DoesNotThrow(() => repo.Unstage(relativePath, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false }));
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void UnstagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Touch(di.FullName, filename, "some contents");

                Assert.Throws<ArgumentException>(() => repo.Unstage(fullPath));
            }
        }

        [Fact]
        public void UnstagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirAgainstAnOrphanedHeadThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Touch(di.FullName, filename, "some contents");

                Assert.Throws<ArgumentException>(() => repo.Unstage(fullPath));
            }
        }

        [Fact]
        public void UnstagingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Unstage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Unstage((string)null));
                Assert.Throws<ArgumentException>(() => repo.Unstage(new string[] { }));
                Assert.Throws<ArgumentException>(() => repo.Unstage(new string[] { null }));
            }
        }

        [Fact]
        public void CanUnstageSourceOfARename()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Move("branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Equal(1, oldStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.Nonexistent, oldStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                repo.Unstage(new string[] { "branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(0, newStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.Missing, newStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.Added, newStatus["renamed_branch_file.txt"].State);
            }
        }

        [Fact]
        public void CanUnstageTargetOfARename()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Move("branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Equal(1, oldStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                repo.Unstage(new string[] { "renamed_branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(0, newStatus.RenamedInIndex.Count());
                Assert.Equal(FileStatus.Untracked, newStatus["renamed_branch_file.txt"].State);
                Assert.Equal(FileStatus.Removed, newStatus["branch_file.txt"].State);
            }
        }

        [Fact]
        public void CanUnstageBothSidesOfARename()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                repo.Move("branch_file.txt", "renamed_branch_file.txt");
                repo.Unstage(new string[] { "branch_file.txt", "renamed_branch_file.txt" });

                RepositoryStatus status = repo.RetrieveStatus();
                Assert.Equal(FileStatus.Missing, status["branch_file.txt"].State);
                Assert.Equal(FileStatus.Untracked, status["renamed_branch_file.txt"].State);
            }
        }
    }
}
