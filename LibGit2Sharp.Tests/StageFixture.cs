using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class StageFixture : BaseFixture
    {
        [Theory]
        [InlineData("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [InlineData("README", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [InlineData("deleted_unstaged_file.txt", FileStatus.Missing, true, FileStatus.Removed, false, -1)]
        [InlineData("modified_unstaged_file.txt", FileStatus.Modified, true, FileStatus.Staged, true, 0)]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked, false, FileStatus.Added, true, 1)]
        [InlineData("modified_staged_file.txt", FileStatus.Staged, true, FileStatus.Staged, true, 0)]
        [InlineData("new_tracked_file.txt", FileStatus.Added, true, FileStatus.Added, true, 0)]
        public void CanStage(string relativePath, FileStatus currentStatus, bool doesCurrentlyExistInTheIndex, FileStatus expectedStatusOnceStaged, bool doesExistInTheIndexOnceStaged, int expectedIndexCountVariation)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;
                Assert.Equal(doesCurrentlyExistInTheIndex, (repo.Index[relativePath] != null));
                Assert.Equal(currentStatus, repo.Index.RetrieveStatus(relativePath));

                repo.Index.Stage(relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
                Assert.Equal(doesExistInTheIndexOnceStaged, (repo.Index[relativePath] != null));
                Assert.Equal(expectedStatusOnceStaged, repo.Index.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void CanStageTheUpdationOfAStagedFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;
                const string filename = "new_tracked_file.txt";
                IndexEntry blob = repo.Index[filename];

                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));

                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "brand new content");
                Assert.Equal(FileStatus.Added | FileStatus.Modified, repo.Index.RetrieveStatus(filename));

                repo.Index.Stage(filename);
                IndexEntry newBlob = repo.Index[filename];

                Assert.Equal(count, repo.Index.Count);
                Assert.NotEqual(newBlob.Id, blob.Id);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));
            }
        }

        [Theory]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed)]
        public void StagingAnUnknownFileThrows(string relativePath, FileStatus status)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));

                Assert.Throws<LibGit2SharpException>(() => repo.Index.Stage(relativePath));
            }
        }

        [Fact]
        public void CanStageTheRemovalOfAStagedFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;
                const string filename = "new_tracked_file.txt";
                Assert.NotNull(repo.Index[filename]);

                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));

                File.Delete(Path.Combine(repo.Info.WorkingDirectory, filename));
                Assert.Equal(FileStatus.Added | FileStatus.Missing, repo.Index.RetrieveStatus(filename));

                repo.Index.Stage(filename);
                Assert.Null(repo.Index[filename]);

                Assert.Equal(count - 1, repo.Index.Count);
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus(filename));
            }
        }

        [Fact]
        public void CanStageANewFileInAPersistentManner()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string filename = "unit_test.txt";
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "some contents");
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                repo.Index.Stage(filename);
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));
            }

            using (var repo = new Repository(path.RepositoryPath))
            {
                const string filename = "unit_test.txt";
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));
            }
        }

        [SkippableTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanStageANewFileWithAFullPath(bool ignorecase)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            SetIgnoreCaseOrSkip(path.RepositoryPath, ignorecase);

            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                const string filename = "new_untracked_file.txt";
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                Assert.True(File.Exists(fullPath));

                AssertStage(null, repo, fullPath);
                AssertStage(ignorecase, repo, fullPath.ToUpperInvariant());
                AssertStage(ignorecase, repo, fullPath.ToLowerInvariant());
            }
        }

        private static void AssertStage(bool? ignorecase, IRepository repo, string path)
        {
            try
            {
                repo.Index.Stage(path);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(path));
                repo.Reset();
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(path));
            }
            catch (ArgumentException)
            {
                Assert.False(ignorecase ?? true);
            }
        }

        [Fact]
        public void CanStageANewFileWithARelativePathContainingNativeDirectorySeparatorCharacters()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                DirectoryInfo di = Directory.CreateDirectory(Path.Combine(repo.Info.WorkingDirectory, "Project"));
                string file = Path.Combine("Project", "a_file.txt");

                File.WriteAllText(Path.Combine(di.FullName, "a_file.txt"), "With backward slash on Windows!");

                repo.Index.Stage(file);

                Assert.Equal(count + 1, repo.Index.Count);

                const string posixifiedPath = "Project/a_file.txt";
                Assert.NotNull(repo.Index[posixifiedPath]);
                Assert.Equal(file, repo.Index[posixifiedPath].Path);
            }
        }

        [Fact]
        public void StagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Path.Combine(di.FullName, filename);
                File.WriteAllText(fullPath, "some contents");

                Assert.Throws<ArgumentException>(() => repo.Index.Stage(fullPath));
            }
        }

        [Fact]
        public void StageFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage((string)null));
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(new string[] { }));
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(new string[] { null }));
            }
        }
    }
}
