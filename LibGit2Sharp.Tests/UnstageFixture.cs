using System;
using System.IO;
using System.Linq;
using System.Text;
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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                string filename = Path.Combine("1", "branch_file.txt");
                const string posixifiedFileName = "1/branch_file.txt";
                ObjectId blobId = repo.Index[posixifiedFileName].Id;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                File.AppendAllText(fullpath, "Is there there anybody out there?");
                repo.Index.Stage(filename);

                Assert.Equal(count, repo.Index.Count);
                Assert.NotEqual((blobId), repo.Index[posixifiedFileName].Id);

                repo.Index.Unstage(posixifiedFileName);

                Assert.Equal(count, repo.Index.Count);
                Assert.Equal(blobId, repo.Index[posixifiedFileName].Id);
            }
        }

        [Fact]
        public void CanStageAndUnstageAnIgnoredFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                string gitignorePath = Path.Combine(repo.Info.WorkingDirectory, ".gitignore");
                File.WriteAllText(gitignorePath, "*.ign" + Environment.NewLine);

                const string relativePath = "Champa.ign";
                string gitignoredFile = Path.Combine(repo.Info.WorkingDirectory, relativePath);
                File.WriteAllText(gitignoredFile, "On stage!" + Environment.NewLine);

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(relativePath));

                repo.Index.Stage(relativePath);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(relativePath));

                repo.Index.Unstage(relativePath);
                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [InlineData("deleted_unstaged_file.txt", FileStatus.Missing, true, FileStatus.Missing, true, 0)]
        [InlineData("modified_unstaged_file.txt", FileStatus.Modified, true, FileStatus.Modified, true, 0)]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked, false, FileStatus.Untracked, false, 0)]
        [InlineData("modified_staged_file.txt", FileStatus.Staged, true, FileStatus.Modified, true, 0)]
        [InlineData("new_tracked_file.txt", FileStatus.Added, true, FileStatus.Untracked, false, -1)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent, false, FileStatus.Nonexistent, false, 0)]
        public void CanUnStage(string relativePath, FileStatus currentStatus, bool doesCurrentlyExistInTheIndex, FileStatus expectedStatusOnceStaged, bool doesExistInTheIndexOnceStaged, int expectedIndexCountVariation)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;
                Assert.Equal(doesCurrentlyExistInTheIndex, (repo.Index[relativePath] != null));
                Assert.Equal(currentStatus, repo.Index.RetrieveStatus(relativePath));

                repo.Index.Unstage(relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
                Assert.Equal(doesExistInTheIndexOnceStaged, (repo.Index[relativePath] != null));
                Assert.Equal(expectedStatusOnceStaged, repo.Index.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void CanUnstageTheRemovalOfAFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                const string filename = "deleted_staged_file.txt";

                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                Assert.False(File.Exists(fullPath));

                Assert.Equal(FileStatus.Removed, repo.Index.RetrieveStatus(filename));

                repo.Index.Unstage(filename);
                Assert.Equal(count + 1, repo.Index.Count);

                Assert.Equal(FileStatus.Missing, repo.Index.RetrieveStatus(filename));
            }
        }

        [Fact]
        public void CanUnstageUntrackedFileAgainstAnOrphanedHead()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                string relativePath = "a.txt";
                string absolutePath = Path.Combine(repo.Info.WorkingDirectory, relativePath);

                File.WriteAllText(absolutePath, "hello test file\n", Encoding.ASCII);
                repo.Index.Stage(absolutePath);

                repo.Index.Unstage(relativePath);
                RepositoryStatus status = repo.Index.RetrieveStatus();
                Assert.Equal(0, status.Staged.Count());
                Assert.Equal(1, status.Untracked.Count());
            }
        }

        [Fact]
        public void UnstagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Path.Combine(di.FullName, filename);
                File.WriteAllText(fullPath, "some contents");

                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(fullPath));
            }
        }

        [Fact]
        public void UnstagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirAgainstAnOrphanedHeadThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            SelfCleaningDirectory scd2 = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd2.DirectoryPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Path.Combine(di.FullName, filename);
                File.WriteAllText(fullPath, "some contents");

                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(fullPath));
            }
        }

        [Fact]
        public void UnstagingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Unstage((string)null));
                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(new string[] { }));
                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(new string[] { null }));
            }
        }
    }
}
