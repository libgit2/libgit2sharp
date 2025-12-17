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
        [InlineData("deleted_unstaged_file.txt", FileStatus.DeletedFromWorkdir, true, FileStatus.DeletedFromIndex, false, -1)]
        [InlineData("modified_unstaged_file.txt", FileStatus.ModifiedInWorkdir, true, FileStatus.ModifiedInIndex, true, 0)]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir, false, FileStatus.NewInIndex, true, 1)]
        [InlineData("modified_staged_file.txt", FileStatus.ModifiedInIndex, true, FileStatus.ModifiedInIndex, true, 0)]
        [InlineData("new_tracked_file.txt", FileStatus.NewInIndex, true, FileStatus.NewInIndex, true, 0)]
        public void CanStage(string relativePath, FileStatus currentStatus, bool doesCurrentlyExistInTheIndex, FileStatus expectedStatusOnceStaged, bool doesExistInTheIndexOnceStaged, int expectedIndexCountVariation)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;
                Assert.Equal(doesCurrentlyExistInTheIndex, (repo.Index[relativePath] != null));
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Commands.Stage(repo, relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
                Assert.Equal(doesExistInTheIndexOnceStaged, (repo.Index[relativePath] != null));
                Assert.Equal(expectedStatusOnceStaged, repo.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("deleted_unstaged_file.txt", FileStatus.DeletedFromIndex)]
        [InlineData("modified_unstaged_file.txt", FileStatus.ModifiedInIndex)]
        [InlineData("new_untracked_file.txt", FileStatus.NewInIndex)]
        public void StagingWritesIndex(string relativePath, FileStatus expectedStatus)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Commands.Stage(repo, relativePath);
            }

            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedStatus, repo.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void CanStageTheUpdationOfAStagedFile()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;
                const string filename = "new_tracked_file.txt";
                IndexEntry blob = repo.Index[filename];

                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));

                Touch(repo.Info.WorkingDirectory, filename, "brand new content");
                Assert.Equal(FileStatus.NewInIndex | FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(filename));

                Commands.Stage(repo, filename);
                IndexEntry newBlob = repo.Index[filename];

                Assert.Equal(count, repo.Index.Count);
                Assert.NotEqual(newBlob.Id, blob.Id);
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));
            }
        }

        [Theory]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromIndex)]
        public void StagingAnUnknownFileThrowsIfExplicitPath(string relativePath, FileStatus status)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.RetrieveStatus(relativePath));

                Assert.Throws<UnmatchedPathException>(() => Commands.Stage(repo, relativePath, new StageOptions { ExplicitPathsOptions = new ExplicitPathsOptions() }));
            }
        }

        [Theory]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromIndex)]
        public void CanStageAnUnknownFileWithLaxUnmatchedExplicitPathsValidation(string relativePath, FileStatus status)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.RetrieveStatus(relativePath));

                Commands.Stage(repo, relativePath);
                Commands.Stage(repo, relativePath, new StageOptions { ExplicitPathsOptions = new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false } });

                Assert.Equal(status, repo.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromIndex)]
        public void StagingAnUnknownFileWithLaxExplicitPathsValidationDoesntThrow(string relativePath, FileStatus status)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.RetrieveStatus(relativePath));

                Commands.Stage(repo, relativePath);
                Commands.Stage(repo, relativePath, new StageOptions { ExplicitPathsOptions = new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false } });
            }
        }

        [Fact]
        public void CanStageTheRemovalOfAStagedFile()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;
                const string filename = "new_tracked_file.txt";
                Assert.NotNull(repo.Index[filename]);

                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));

                File.Delete(Path.Combine(repo.Info.WorkingDirectory, filename));
                Assert.Equal(FileStatus.NewInIndex | FileStatus.DeletedFromWorkdir, repo.RetrieveStatus(filename));

                Commands.Stage(repo, filename);
                Assert.Null(repo.Index[filename]);

                Assert.Equal(count - 1, repo.Index.Count);
                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus(filename));
            }
        }

        [Theory]
        [InlineData("unit_test.txt")]
        [InlineData("!unit_test.txt")]
        [InlineData("!bang/unit_test.txt")]
        public void CanStageANewFileInAPersistentManner(string filename)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                Touch(repo.Info.WorkingDirectory, filename, "some contents");
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                Commands.Stage(repo, filename);
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));
            }

            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));
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

            string path = SandboxStandardTestRepo();

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
                Commands.Stage(repo, path);
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(path));
                repo.Index.Replace(repo.Head.Tip);
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(path));
            }
            catch (ArgumentException)
            {
                Assert.False(ignorecase ?? true);
            }
        }

        [Fact]
        public void CanStageANewFileWithARelativePathContainingNativeDirectorySeparatorCharacters()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string file = Path.Combine("Project", "a_file.txt");

                Touch(repo.Info.WorkingDirectory, file, "With backward slash on Windows!");

                Commands.Stage(repo, file);

                Assert.Equal(count + 1, repo.Index.Count);

                const string posixifiedPath = "Project/a_file.txt";
                Assert.NotNull(repo.Index[posixifiedPath]);
                Assert.Equal(posixifiedPath, repo.Index[posixifiedPath].Path);
            }
        }

        [Fact]
        public void StagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string fullPath = Touch(scd.RootedDirectoryPath, "unit_test.txt", "some contents");

                Assert.Throws<ArgumentException>(() => Commands.Stage(repo, fullPath));
            }
        }

        [Fact]
        public void StagingFileWithBadParamsThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => Commands.Stage(repo, string.Empty));
                Assert.Throws<ArgumentNullException>(() => Commands.Stage(repo, (string)null));
                Assert.Throws<ArgumentException>(() => Commands.Stage(repo, Array.Empty<string>()));
                Assert.Throws<ArgumentException>(() => Commands.Stage(repo, new string[] { null }));
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
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                int count = repo.Index.Count;

                Commands.Stage(repo, relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
            }
        }

        [Fact]
        public void CanStageWithMultiplePathspecs()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                int count = repo.Index.Count;

                Commands.Stage(repo, new string[] { "*", "u*" });

                Assert.Equal(count, repo.Index.Count);  // 1 added file, 1 deleted file, so same count
            }
        }

        [Theory]
        [InlineData("ignored_file.txt")]
        [InlineData("ignored_folder/file.txt")]
        public void CanIgnoreIgnoredPaths(string path)
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Touch(repo.Info.WorkingDirectory, ".gitignore", "ignored_file.txt\nignored_folder/\n");
                Touch(repo.Info.WorkingDirectory, path, "This file is ignored.");

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(path));
                Commands.Stage(repo, "*");
                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(path));
            }
        }

        [Theory]
        [InlineData("ignored_file.txt")]
        [InlineData("ignored_folder/file.txt")]
        public void CanStageIgnoredPaths(string path)
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                Touch(repo.Info.WorkingDirectory, ".gitignore", "ignored_file.txt\nignored_folder/\n");
                Touch(repo.Info.WorkingDirectory, path, "This file is ignored.");

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(path));
                Commands.Stage(repo, path, new StageOptions { IncludeIgnored = true });
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(path));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.Ignored)]
        [InlineData("modified_unstaged_file.txt", FileStatus.ModifiedInIndex)]
        public void IgnoredFilesAreOnlyStagedIfTheyreInTheRepo(string filename, FileStatus expected)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, ".gitignore"),
                    string.Format("{0}\n", filename));

                Commands.Stage(repo, filename);
                Assert.Equal(expected, repo.RetrieveStatus(filename));
            }
        }

        [Theory]
        [InlineData("ancestor-and-ours.txt", FileStatus.Unaltered)]
        [InlineData("ancestor-and-theirs.txt", FileStatus.NewInIndex)]
        [InlineData("ancestor-only.txt", FileStatus.Nonexistent)]
        [InlineData("conflicts-one.txt", FileStatus.ModifiedInIndex)]
        [InlineData("conflicts-two.txt", FileStatus.ModifiedInIndex)]
        [InlineData("ours-only.txt", FileStatus.Unaltered)]
        [InlineData("ours-and-theirs.txt", FileStatus.ModifiedInIndex)]
        [InlineData("theirs-only.txt", FileStatus.NewInIndex)]
        public void CanStageConflictedIgnoredFiles(string filename, FileStatus expected)
        {
            var path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, ".gitignore"),
                    string.Format("{0}\n", filename));

                Commands.Stage(repo, filename);
                Assert.Equal(expected, repo.RetrieveStatus(filename));
            }
        }

        [Fact]
        public void CanSuccessfullyStageTheContentOfAModifiedFileOfTheSameSizeWithinTheSameSecond()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                for (int i = 0; i < 10; i++)
                {
                    Touch(repo.Info.WorkingDirectory, "test.txt",
                               Guid.NewGuid().ToString());

                    Commands.Stage(repo, "test.txt");

                    repo.Commit("Commit", Constants.Signature, Constants.Signature);
                }
            }
        }
    }
}
