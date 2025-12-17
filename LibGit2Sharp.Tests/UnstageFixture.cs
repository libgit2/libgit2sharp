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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string filename = Path.Combine("1", "branch_file.txt");
                const string posixifiedFileName = "1/branch_file.txt";
                ObjectId blobId = repo.Index[posixifiedFileName].Id;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                File.AppendAllText(fullpath, "Is there there anybody out there?");
                Commands.Stage(repo, filename);

                Assert.Equal(count, repo.Index.Count);
                Assert.NotEqual((blobId), repo.Index[posixifiedFileName].Id);

                Commands.Unstage(repo, posixifiedFileName);

                Assert.Equal(count, repo.Index.Count);
                Assert.Equal(blobId, repo.Index[posixifiedFileName].Id);
            }
        }

        [Fact]
        public void CanStageAndUnstageAnIgnoredFile()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, ".gitignore", "*.ign" + Environment.NewLine);

                const string relativePath = "Champa.ign";
                Touch(repo.Info.WorkingDirectory, relativePath, "On stage!" + Environment.NewLine);

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(relativePath));

                Commands.Stage(repo, relativePath, new StageOptions { IncludeIgnored = true });
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(relativePath));

                Commands.Unstage(repo, relativePath);
                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [InlineData("deleted_unstaged_file.txt", FileStatus.DeletedFromWorkdir, true, FileStatus.DeletedFromWorkdir, true, 0)]
        [InlineData("modified_unstaged_file.txt", FileStatus.ModifiedInWorkdir, true, FileStatus.ModifiedInWorkdir, true, 0)]
        [InlineData("modified_staged_file.txt", FileStatus.ModifiedInIndex, true, FileStatus.ModifiedInWorkdir, true, 0)]
        [InlineData("new_tracked_file.txt", FileStatus.NewInIndex, true, FileStatus.NewInWorkdir, false, -1)]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromIndex, false, FileStatus.DeletedFromWorkdir, true, 1)]
        public void CanUnstage(
            string relativePath, FileStatus currentStatus, bool doesCurrentlyExistInTheIndex,
            FileStatus expectedStatusOnceStaged, bool doesExistInTheIndexOnceStaged, int expectedIndexCountVariation)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;
                Assert.Equal(doesCurrentlyExistInTheIndex, (repo.Index[relativePath] != null));
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Commands.Unstage(repo, relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
                Assert.Equal(doesExistInTheIndexOnceStaged, (repo.Index[relativePath] != null));
                Assert.Equal(expectedStatusOnceStaged, repo.RetrieveStatus(relativePath));
            }
        }


        [Theory]
        [InlineData("modified_staged_file.txt", FileStatus.ModifiedInWorkdir)]
        [InlineData("new_tracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromWorkdir)]
        public void UnstagingWritesIndex(string relativePath, FileStatus expectedStatus)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Commands.Unstage(repo, relativePath);
            }

            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedStatus, repo.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void UnstagingUnknownPathsWithStrictUnmatchedExplicitPathsValidationThrows(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Assert.Throws<UnmatchedPathException>(() => Commands.Unstage(repo, relativePath, new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void CanUnstageUnknownPathsWithLaxUnmatchedExplicitPathsValidation(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Commands.Unstage(repo, relativePath, new ExplicitPathsOptions() { ShouldFailOnUnmatchedPath = false });

                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void CanUnstageTheRemovalOfAFile()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                const string filename = "deleted_staged_file.txt";

                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                Assert.False(File.Exists(fullPath));

                Assert.Equal(FileStatus.DeletedFromIndex, repo.RetrieveStatus(filename));

                Commands.Unstage(repo, filename);
                Assert.Equal(count + 1, repo.Index.Count);

                Assert.Equal(FileStatus.DeletedFromWorkdir, repo.RetrieveStatus(filename));
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

                Commands.Stage(repo, relativePath);

                Commands.Unstage(repo, relativePath);
                RepositoryStatus status = repo.RetrieveStatus();
                Assert.Empty(status.Staged);
                Assert.Single(status.Untracked);

                Assert.Throws<UnmatchedPathException>(() => Commands.Unstage(repo, "i-dont-exist", new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void UnstagingUnknownPathsAgainstAnOrphanedHeadWithStrictUnmatchedExplicitPathsValidationThrows(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/orphaned");
                Assert.True(repo.Info.IsHeadUnborn);

                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Assert.Throws<UnmatchedPathException>(() => Commands.Unstage(repo, relativePath, new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void CanUnstageUnknownPathsAgainstAnOrphanedHeadWithLaxUnmatchedExplicitPathsValidation(string relativePath, FileStatus currentStatus)
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                repo.Refs.UpdateTarget("HEAD", "refs/heads/orphaned");
                Assert.True(repo.Info.IsHeadUnborn);

                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Commands.Unstage(repo, relativePath);
                Commands.Unstage(repo, relativePath, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false });

                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void UnstagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Touch(di.FullName, filename, "some contents");

                Assert.Throws<ArgumentException>(() => Commands.Unstage(repo, fullPath));
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

                Assert.Throws<ArgumentException>(() => Commands.Unstage(repo, fullPath));
            }
        }

        [Fact]
        public void UnstagingFileWithBadParamsThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => Commands.Unstage(repo, string.Empty));
                Assert.Throws<ArgumentNullException>(() => Commands.Unstage(repo, (string)null));
                Assert.Throws<ArgumentException>(() => Commands.Unstage(repo, Array.Empty<string>()));
                Assert.Throws<ArgumentException>(() => Commands.Unstage(repo, new string[] { null }));
            }
        }

        [Fact]
        public void CanUnstageSourceOfARename()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Commands.Move(repo, "branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Single(oldStatus.RenamedInIndex);
                Assert.Equal(FileStatus.Nonexistent, oldStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                Commands.Unstage(repo, new string[] { "branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Empty(newStatus.RenamedInIndex);
                Assert.Equal(FileStatus.DeletedFromWorkdir, newStatus["branch_file.txt"].State);
                Assert.Equal(FileStatus.NewInIndex, newStatus["renamed_branch_file.txt"].State);
            }
        }

        [Fact]
        public void CanUnstageTargetOfARename()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Commands.Move(repo, "branch_file.txt", "renamed_branch_file.txt");

                RepositoryStatus oldStatus = repo.RetrieveStatus();
                Assert.Single(oldStatus.RenamedInIndex);
                Assert.Equal(FileStatus.RenamedInIndex, oldStatus["renamed_branch_file.txt"].State);

                Commands.Unstage(repo, new string[] { "renamed_branch_file.txt" });

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Empty(newStatus.RenamedInIndex);
                Assert.Equal(FileStatus.NewInWorkdir, newStatus["renamed_branch_file.txt"].State);
                Assert.Equal(FileStatus.DeletedFromIndex, newStatus["branch_file.txt"].State);
            }
        }

        [Fact]
        public void CanUnstageBothSidesOfARename()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Commands.Move(repo, "branch_file.txt", "renamed_branch_file.txt");
                Commands.Unstage(repo, new string[] { "branch_file.txt", "renamed_branch_file.txt" });

                RepositoryStatus status = repo.RetrieveStatus();
                Assert.Equal(FileStatus.DeletedFromWorkdir, status["branch_file.txt"].State);
                Assert.Equal(FileStatus.NewInWorkdir, status["renamed_branch_file.txt"].State);
            }
        }
    }
}
