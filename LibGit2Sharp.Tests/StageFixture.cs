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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;
                const string filename = "new_tracked_file.txt";
                IndexEntry blob = repo.Index[filename];

                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));

                Touch(repo.Info.WorkingDirectory, filename, "brand new content");
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
        public void StagingAnUnknownFileThrowsIfExplicitPath(string relativePath, FileStatus status)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));

                Assert.Throws<UnmatchedPathException>(() => repo.Index.Stage(relativePath, new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed)]
        public void CanStageAnUnknownFileWithLaxUnmatchedExplicitPathsValidation(string relativePath, FileStatus status)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));

                Assert.DoesNotThrow(() => repo.Index.Stage(relativePath));
                Assert.DoesNotThrow(() => repo.Index.Stage(relativePath, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false }));

                Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed)]
        public void StagingAnUnknownFileWithLaxExplicitPathsValidationDoesntThrow(string relativePath, FileStatus status)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));

                repo.Index.Stage(relativePath);
                repo.Index.Stage(relativePath, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false });
            }
        }

        [Fact]
        public void CanStageTheRemovalOfAStagedFile()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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

        [Theory]
        [InlineData("unit_test.txt")]
        [InlineData("!unit_test.txt")]
        [InlineData("!bang/unit_test.txt")]
        public void CanStageANewFileInAPersistentManner(string filename)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                Touch(repo.Info.WorkingDirectory, filename, "some contents");
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                repo.Index.Stage(filename);
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));
            }

            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));
            }
        }

        [SkippableTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanStageANewFileWithAFullPath(bool ignorecase)
        {
            // Skipping due to ignorecase issue in libgit2.
            // See: https://github.com/libgit2/libgit2/pull/1689.
            InconclusiveIf(() => ignorecase,
                "Skipping 'ignorecase = true' test due to ignorecase issue in libgit2.");

            //InconclusiveIf(() => IsFileSystemCaseSensitive && ignorecase,
            //    "Skipping 'ignorecase = true' test on case-sensitive file system.");

            string path = CloneStandardTestRepo();

            using (var repo = new Repository(path))
            {
                repo.Config.Set("core.ignorecase", ignorecase);
            }

            using (var repo = new Repository(path))
            {
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string file = Path.Combine("Project", "a_file.txt");

                Touch(repo.Info.WorkingDirectory, file, "With backward slash on Windows!");

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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string fullPath = Touch(scd.RootedDirectoryPath, "unit_test.txt", "some contents");

                Assert.Throws<ArgumentException>(() => repo.Index.Stage(fullPath));
            }
        }

        [Fact]
        public void StagingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage((string)null));
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(new string[] { }));
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(new string[] { null }));
            }
        }

        /*
         * $ git status -s
         *  M 1/branch_file.txt
         *  M README
         *  M branch_file.txt
         * D  deleted_staged_file.txt
         *  D deleted_unstaged_file.txt
         * M  modified_staged_file.txt
         *  M modified_unstaged_file.txt
         *  M new.txt
         * A  new_tracked_file.txt
         * ?? new_untracked_file.txt
         *
         * By passing "*" to Stage, the following files will be added/removed/updated from the index:
         * - deleted_unstaged_file.txt : removed
         * - modified_unstaged_file.txt : updated
         * - new_untracked_file.txt : added
         */
        [Theory]
        [InlineData("*u*", 0)]
        [InlineData("*", 0)]
        [InlineData("1/*", 0)]
        [InlineData("RE*", 0)]
        [InlineData("d*", -1)]
        [InlineData("*modified_unstaged*", 0)]
        [InlineData("new_*file.txt", 1)]
        public void CanStageWithPathspec(string relativePath, int expectedIndexCountVariation)
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                int count = repo.Index.Count;

                repo.Index.Stage(relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
            }
        }

        [Fact]
        public void CanStageWithMultiplePathspecs()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                int count = repo.Index.Count;

                repo.Index.Stage(new string[] { "*", "u*" });

                Assert.Equal(count, repo.Index.Count);  // 1 added file, 1 deleted file, so same count
            }
        }

        [Theory]
        [InlineData("ignored_file.txt")]
        [InlineData("ignored_folder/file.txt")]
        public void CanStageIgnoredPaths(string path)
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                Touch(repo.Info.WorkingDirectory, ".gitignore", "ignored_file.txt\nignored_folder/\n");
                Touch(repo.Info.WorkingDirectory, path, "This file is ignored.");

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(path));
                repo.Index.Stage(path);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(path));
            }
        }
    }
}
