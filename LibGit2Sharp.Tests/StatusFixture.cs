using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class StatusFixture : BaseFixture
    {
        [Fact]
        public void CanRetrieveTheStatusOfAFile()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                FileStatus status = repo.RetrieveStatus("new_tracked_file.txt");
                Assert.Equal(FileStatus.Added, status);
            }
        }

        [Theory]
        [InlineData(StatusShowOption.IndexAndWorkDir, FileStatus.Untracked)]
        [InlineData(StatusShowOption.WorkDirOnly, FileStatus.Untracked)]
        [InlineData(StatusShowOption.IndexOnly, FileStatus.Nonexistent)]
        public void CanLimitStatusToWorkDirOnly(StatusShowOption show, FileStatus expected)
        {
            var clone = CloneStandardTestRepo();

            using (var repo = new Repository(clone))
            {
                Touch(repo.Info.WorkingDirectory, "file.txt", "content");

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { Show = show });
                Assert.Equal(expected, status["file.txt"].State);
            }
        }

        [Theory]
        [InlineData(StatusShowOption.IndexAndWorkDir, FileStatus.Added)]
        [InlineData(StatusShowOption.WorkDirOnly, FileStatus.Nonexistent)]
        [InlineData(StatusShowOption.IndexOnly, FileStatus.Added)]
        public void CanLimitStatusToIndexOnly(StatusShowOption show, FileStatus expected)
        {
            var clone = CloneStandardTestRepo();

            using (var repo = new Repository(clone))
            {
                Touch(repo.Info.WorkingDirectory, "file.txt", "content");
                repo.Stage("file.txt");

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { Show = show });
                Assert.Equal(expected, status["file.txt"].State);
            }
        }


        [Theory]
        [InlineData("file")]
        [InlineData("file.txt")]
        [InlineData("$file")]
        [InlineData("$file.txt")]
        [InlineData("$dir/file")]
        [InlineData("$dir/file.txt")]
        [InlineData("#file")]
        [InlineData("#file.txt")]
        [InlineData("#dir/file")]
        [InlineData("#dir/file.txt")]
        [InlineData("^file")]
        [InlineData("^file.txt")]
        [InlineData("^dir/file")]
        [InlineData("^dir/file.txt")]
        [InlineData("!file")]
        [InlineData("!file.txt")]
        [InlineData("!dir/file")]
        [InlineData("!dir/file.txt")]
        [InlineData("file!")]
        [InlineData("file!.txt")]
        [InlineData("dir!/file")]
        [InlineData("dir!/file.txt")]
        public void CanRetrieveTheStatusOfAnUntrackedFile(string filePath)
        {
            var clone = CloneStandardTestRepo();

            using (var repo = new Repository(clone))
            {
                Touch(repo.Info.WorkingDirectory, filePath, "content");

                FileStatus status = repo.RetrieveStatus(filePath);
                Assert.Equal(FileStatus.Untracked, status);
            }
        }

        [Fact]
        public void RetrievingTheStatusOfADirectoryThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<AmbiguousSpecificationException>(() => { FileStatus status = repo.RetrieveStatus("1"); });
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfTheWholeWorkingDirectory()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string file = "modified_staged_file.txt";

                RepositoryStatus status = repo.RetrieveStatus();

                Assert.Equal(FileStatus.Staged, status[file].State);

                Assert.NotNull(status);
                Assert.Equal(6, status.Count());
                Assert.True(status.IsDirty);

                Assert.Equal("new_untracked_file.txt", status.Untracked.Select(s => s.FilePath).Single());
                Assert.Equal("modified_unstaged_file.txt", status.Modified.Select(s => s.FilePath).Single());
                Assert.Equal("deleted_unstaged_file.txt", status.Missing.Select(s => s.FilePath).Single());
                Assert.Equal("new_tracked_file.txt", status.Added.Select(s => s.FilePath).Single());
                Assert.Equal(file, status.Staged.Select(s => s.FilePath).Single());
                Assert.Equal("deleted_staged_file.txt", status.Removed.Select(s => s.FilePath).Single());

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, file),
                                   "Tclem's favorite commit message: boom");

                Assert.Equal(FileStatus.Staged | FileStatus.Modified, repo.RetrieveStatus(file));

                RepositoryStatus status2 = repo.RetrieveStatus();
                Assert.Equal(FileStatus.Staged | FileStatus.Modified, status2[file].State);

                Assert.NotNull(status2);
                Assert.Equal(6, status2.Count());
                Assert.True(status2.IsDirty);

                Assert.Equal("new_untracked_file.txt", status2.Untracked.Select(s => s.FilePath).Single());
                Assert.Equal(new[] { file, "modified_unstaged_file.txt" }, status2.Modified.Select(s => s.FilePath));
                Assert.Equal("deleted_unstaged_file.txt", status2.Missing.Select(s => s.FilePath).Single());
                Assert.Equal("new_tracked_file.txt", status2.Added.Select(s => s.FilePath).Single());
                Assert.Equal(file, status2.Staged.Select(s => s.FilePath).Single());
                Assert.Equal("deleted_staged_file.txt", status2.Removed.Select(s => s.FilePath).Single());
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfRenamedFilesInWorkDir()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "old_name.txt",
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n");

                repo.Stage("old_name.txt");

                File.Move(Path.Combine(repo.Info.WorkingDirectory, "old_name.txt"),
                    Path.Combine(repo.Info.WorkingDirectory, "rename_target.txt"));

                RepositoryStatus status = repo.RetrieveStatus(
                    new StatusOptions()
                    {
                        DetectRenamesInIndex = true,
                        DetectRenamesInWorkDir = true
                    });

                Assert.Equal(FileStatus.Added | FileStatus.RenamedInWorkDir, status["rename_target.txt"].State);
                Assert.Equal(100, status["rename_target.txt"].IndexToWorkDirRenameDetails.Similarity);
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfRenamedFilesInIndex()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                File.Move(
                    Path.Combine(repo.Info.WorkingDirectory, "1.txt"),
                    Path.Combine(repo.Info.WorkingDirectory, "rename_target.txt"));

                repo.Stage("1.txt");
                repo.Stage("rename_target.txt");

                RepositoryStatus status = repo.RetrieveStatus();

                Assert.Equal(FileStatus.RenamedInIndex, status["rename_target.txt"].State);
                Assert.Equal(100, status["rename_target.txt"].HeadToIndexRenameDetails.Similarity);
            }
        }

        [Fact]
        public void CanDetectedVariousKindsOfRenaming()
        {
            string path = InitNewRepository();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "file.txt",
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n");

                repo.Stage("file.txt");
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                File.Move(Path.Combine(repo.Info.WorkingDirectory, "file.txt"),
                    Path.Combine(repo.Info.WorkingDirectory, "renamed.txt"));

                var opts = new StatusOptions
                {
                    DetectRenamesInIndex = true,
                    DetectRenamesInWorkDir = true
                };

                RepositoryStatus status = repo.RetrieveStatus(opts);

                // This passes as expected
                Assert.Equal(FileStatus.RenamedInWorkDir, status.Single().State);

                repo.Stage("file.txt");
                repo.Stage("renamed.txt");

                status = repo.RetrieveStatus(opts);

                Assert.Equal(FileStatus.RenamedInIndex, status.Single().State);

                File.Move(Path.Combine(repo.Info.WorkingDirectory, "renamed.txt"),
                    Path.Combine(repo.Info.WorkingDirectory, "renamed_again.txt"));

                status = repo.RetrieveStatus(opts);

                Assert.Equal(FileStatus.RenamedInWorkDir | FileStatus.RenamedInIndex,
                    status.Single().State);
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfANewRepository()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                RepositoryStatus status = repo.RetrieveStatus();
                Assert.NotNull(status);
                Assert.Equal(0, status.Count());
                Assert.False(status.IsDirty);

                Assert.Equal(0, status.Untracked.Count());
                Assert.Equal(0, status.Modified.Count());
                Assert.Equal(0, status.Missing.Count());
                Assert.Equal(0, status.Added.Count());
                Assert.Equal(0, status.Staged.Count());
                Assert.Equal(0, status.Removed.Count());
            }
        }

        [Fact]
        public void RetrievingTheStatusOfARepositoryReturnNativeFilePaths()
        {
            // Build relative path
            string relFilePath = Path.Combine("directory", "Testfile.txt");

            // Open the repository
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.WorkingDirectory, relFilePath, "Anybody out there?");

                // Add the file to the index
                repo.Stage(relFilePath);

                // Get the repository status
                RepositoryStatus repoStatus = repo.RetrieveStatus();

                Assert.Equal(1, repoStatus.Count());
                StatusEntry statusEntry = repoStatus.Single();

                Assert.Equal(relFilePath, statusEntry.FilePath);

                Assert.Equal(statusEntry.FilePath, repoStatus.Added.Select(s => s.FilePath).Single());
            }
        }

        [Fact]
        public void RetrievingTheStatusOfAnEmptyRepositoryHonorsTheGitIgnoreDirectives()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                const string relativePath = "look-ma.txt";
                Touch(repo.Info.WorkingDirectory, relativePath, "I'm going to be ignored!");

                RepositoryStatus status = repo.RetrieveStatus();
                Assert.Equal(new[] { relativePath }, status.Untracked.Select(s => s.FilePath));

                Touch(repo.Info.WorkingDirectory, ".gitignore", "*.txt" + Environment.NewLine);

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(".gitignore", newStatus.Untracked.Select(s => s.FilePath).Single());

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(relativePath));
                Assert.Equal(new[] { relativePath }, newStatus.Ignored.Select(s => s.FilePath));
            }
        }

        [Fact]
        public void RetrievingTheStatusOfTheRepositoryHonorsTheGitIgnoreDirectives()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string relativePath = Path.Combine("1", "look-ma.txt");
                Touch(repo.Info.WorkingDirectory, relativePath, "I'm going to be ignored!");

                /*
                 * $ git status --ignored
                 * # On branch master
                 * # Your branch and 'origin/master' have diverged,
                 * # and have 2 and 2 different commit(s) each, respectively.
                 * #
                 * # Changes to be committed:
                 * #   (use "git reset HEAD <file>..." to unstage)
                 * #
                 * #       deleted:    deleted_staged_file.txt
                 * #       modified:   modified_staged_file.txt
                 * #       new file:   new_tracked_file.txt
                 * #
                 * # Changes not staged for commit:
                 * #   (use "git add/rm <file>..." to update what will be committed)
                 * #   (use "git checkout -- <file>..." to discard changes in working directory)
                 * #
                 * #       modified:   1/branch_file.txt
                 * #       modified:   README
                 * #       modified:   branch_file.txt
                 * #       deleted:    deleted_unstaged_file.txt
                 * #       modified:   modified_unstaged_file.txt
                 * #       modified:   new.txt
                 * #
                 * # Untracked files:
                 * #   (use "git add <file>..." to include in what will be committed)
                 * #
                 * #       1/look-ma.txt
                 * #       new_untracked_file.txt
                 */

                RepositoryStatus status = repo.RetrieveStatus();

                Assert.Equal(new[] { relativePath, "new_untracked_file.txt" }, status.Untracked.Select(s => s.FilePath));

                Touch(repo.Info.WorkingDirectory, ".gitignore", "*.txt" + Environment.NewLine);

                /*
                 * $ git status --ignored
                 * # On branch master
                 * # Your branch and 'origin/master' have diverged,
                 * # and have 2 and 2 different commit(s) each, respectively.
                 * #
                 * # Changes to be committed:
                 * #   (use "git reset HEAD <file>..." to unstage)
                 * #
                 * #       deleted:    deleted_staged_file.txt
                 * #       modified:   modified_staged_file.txt
                 * #       new file:   new_tracked_file.txt
                 * #
                 * # Changes not staged for commit:
                 * #   (use "git add/rm <file>..." to update what will be committed)
                 * #   (use "git checkout -- <file>..." to discard changes in working directory)
                 * #
                 * #       modified:   1/branch_file.txt
                 * #       modified:   README
                 * #       modified:   branch_file.txt
                 * #       deleted:    deleted_unstaged_file.txt
                 * #       modified:   modified_unstaged_file.txt
                 * #       modified:   new.txt
                 * #
                 * # Untracked files:
                 * #   (use "git add <file>..." to include in what will be committed)
                 * #
                 * #       .gitignore
                 * # Ignored files:
                 * #   (use "git add -f <file>..." to include in what will be committed)
                 * #
                 * #       1/look-ma.txt
                 * #       new_untracked_file.txt
                 */

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(".gitignore", newStatus.Untracked.Select(s => s.FilePath).Single());

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus(relativePath));
                Assert.Equal(new[] { relativePath, "new_untracked_file.txt" }, newStatus.Ignored.Select(s => s.FilePath));
            }
        }

        [Fact]
        public void RetrievingTheStatusOfAnAmbiguousFileThrows()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "1/ambiguous1.txt", "I don't like brackets.");

                string relativePath = Path.Combine("1", "ambiguous[1].txt");
                Touch(repo.Info.WorkingDirectory, relativePath, "Brackets all the way.");

                Assert.Throws<AmbiguousSpecificationException>(() => repo.RetrieveStatus(relativePath));
            }
        }

        [Theory]
        [InlineData(true, FileStatus.Unaltered, FileStatus.Unaltered)]
        [InlineData(false, FileStatus.Missing, FileStatus.Untracked)]
        public void RetrievingTheStatusOfAFilePathHonorsTheIgnoreCaseConfigurationSetting(
            bool shouldIgnoreCase,
            FileStatus expectedlowerCasedFileStatus,
            FileStatus expectedCamelCasedFileStatus
            )
        {
            string lowerCasedPath;
            const string lowercasedFilename = "plop";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                repo.Config.Set("core.ignorecase", shouldIgnoreCase);

                lowerCasedPath = Touch(repo.Info.WorkingDirectory, lowercasedFilename);

                repo.Stage(lowercasedFilename);
                repo.Commit("initial", Constants.Signature, Constants.Signature);
            }

            using (var repo = new Repository(repoPath))
            {
                const string upercasedFilename = "Plop";

                string camelCasedPath = Path.Combine(repo.Info.WorkingDirectory, upercasedFilename);
                File.Move(lowerCasedPath, camelCasedPath);

                Assert.Equal(expectedlowerCasedFileStatus, repo.RetrieveStatus(lowercasedFilename));
                Assert.Equal(expectedCamelCasedFileStatus, repo.RetrieveStatus(upercasedFilename));

                AssertStatus(shouldIgnoreCase, expectedlowerCasedFileStatus, repo, camelCasedPath.ToLowerInvariant());
                AssertStatus(shouldIgnoreCase, expectedCamelCasedFileStatus, repo, camelCasedPath.ToUpperInvariant());
            }
        }

        private static void AssertStatus(bool shouldIgnoreCase, FileStatus expectedFileStatus, IRepository repo, string path)
        {
            try
            {
                Assert.Equal(expectedFileStatus, repo.RetrieveStatus(path));
            }
            catch (ArgumentException)
            {
                Assert.False(shouldIgnoreCase);
            }
        }

        [Fact]
        public void RetrievingTheStatusOfTheRepositoryHonorsTheGitIgnoreDirectivesThroughoutDirectories()
        {
            char dirSep = Path.DirectorySeparatorChar;

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "bin/look-ma.txt", "I'm going to be ignored!");
                Touch(repo.Info.WorkingDirectory, "bin/what-about-me.txt", "Huh?");

                const string gitIgnore = ".gitignore";
                Touch(repo.Info.WorkingDirectory, gitIgnore, "bin");

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus("bin/look-ma.txt"));
                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus("bin/what-about-me.txt"));

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(new[] { "bin" + dirSep }, newStatus.Ignored.Select(s => s.FilePath));

                var sb = new StringBuilder();
                sb.AppendLine("bin/*");
                sb.AppendLine("!bin/w*");
                Touch(repo.Info.WorkingDirectory, gitIgnore, sb.ToString());

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus("bin/look-ma.txt"));
                Assert.Equal(FileStatus.Untracked, repo.RetrieveStatus("bin/what-about-me.txt"));

                newStatus = repo.RetrieveStatus();

                Assert.Equal(new[] { "bin" + dirSep + "look-ma.txt" }, newStatus.Ignored.Select(s => s.FilePath));
                Assert.True(newStatus.Untracked.Select(s => s.FilePath).Contains("bin" + dirSep + "what-about-me.txt"));
            }
        }

        [Fact]
        public void CanRetrieveStatusOfFilesInSubmodule()
        {
            var path = CloneSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                string[] expected = new string[] {
                    ".gitmodules",
                    "sm_changed_file",
                    "sm_changed_head",
                    "sm_changed_index",
                    "sm_changed_untracked_file",
                    "sm_missing_commits"
                };

                RepositoryStatus status = repo.RetrieveStatus();
                Assert.Equal(expected, status.Modified.Select(x => x.FilePath).ToArray());
            }
        }

        [Fact]
        public void CanExcludeStatusOfFilesInSubmodule()
        {
            var path = CloneSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                string[] expected = new string[] {
                    ".gitmodules",
                };

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { ExcludeSubmodules = true });
                Assert.Equal(expected, status.Modified.Select(x => x.FilePath).ToArray());
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfARelativeWorkingDirectory()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string file = "just_a_dir/other.txt";
                const string otherFile = "just_a_dir/another_dir/other.txt";

                Touch(repo.Info.WorkingDirectory, file);
                Touch(repo.Info.WorkingDirectory, otherFile);

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { PathSpec = new[] { "just_a_dir" } });
                Assert.Equal(2, status.Count());
                Assert.Equal(2, status.Untracked.Count());

                status = repo.RetrieveStatus(new StatusOptions() { PathSpec = new[] { "just_a_dir/another_dir" } });
                Assert.Equal(1, status.Count());
                Assert.Equal(1, status.Untracked.Count());
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfMultiplePathSpec()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string file = "just_a_dir/other.txt";
                const string otherFile = "just_a_file.txt";

                Touch(repo.Info.WorkingDirectory, file);
                Touch(repo.Info.WorkingDirectory, otherFile);

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { PathSpec = new[] { "just_a_file.txt", "just_a_dir" } });
                Assert.Equal(2, status.Count());
                Assert.Equal(2, status.Untracked.Count());
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfAGlobSpec()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string file = "just_a_dir/other.txt";
                const string otherFile = "just_a_file.txt";

                Touch(repo.Info.WorkingDirectory, file);
                Touch(repo.Info.WorkingDirectory, otherFile);

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { PathSpec = new[] { "just_a_*" } });
                Assert.Equal(2, status.Count());
                Assert.Equal(2, status.Untracked.Count());
            }
        }
    }
}
