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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                FileStatus status = repo.RetrieveStatus("new_tracked_file.txt");
                Assert.Equal(FileStatus.NewInIndex, status);
            }
        }

        [Theory]
        [InlineData(StatusShowOption.IndexAndWorkDir, FileStatus.NewInWorkdir)]
        [InlineData(StatusShowOption.WorkDirOnly, FileStatus.NewInWorkdir)]
        [InlineData(StatusShowOption.IndexOnly, FileStatus.Nonexistent)]
        public void CanLimitStatusToWorkDirOnly(StatusShowOption show, FileStatus expected)
        {
            var clone = SandboxStandardTestRepo();

            using (var repo = new Repository(clone))
            {
                Touch(repo.Info.WorkingDirectory, "file.txt", "content");

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { Show = show });
                Assert.Equal(expected, status["file.txt"].State);
            }
        }

        [Theory]
        [InlineData(StatusShowOption.IndexAndWorkDir, FileStatus.NewInIndex)]
        [InlineData(StatusShowOption.WorkDirOnly, FileStatus.Nonexistent)]
        [InlineData(StatusShowOption.IndexOnly, FileStatus.NewInIndex)]
        public void CanLimitStatusToIndexOnly(StatusShowOption show, FileStatus expected)
        {
            var clone = SandboxStandardTestRepo();

            using (var repo = new Repository(clone))
            {
                Touch(repo.Info.WorkingDirectory, "file.txt", "content");
                Commands.Stage(repo, "file.txt");

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
            var clone = SandboxStandardTestRepo();

            using (var repo = new Repository(clone))
            {
                Touch(repo.Info.WorkingDirectory, filePath, "content");

                FileStatus status = repo.RetrieveStatus(filePath);
                Assert.Equal(FileStatus.NewInWorkdir, status);
            }
        }

        [Fact]
        public void RetrievingTheStatusOfADirectoryThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Throws<AmbiguousSpecificationException>(() => { repo.RetrieveStatus("1"); });
            }
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 5)]
        public void CanRetrieveTheStatusOfTheWholeWorkingDirectory(bool includeUnaltered, int unalteredCount)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string file = "modified_staged_file.txt";

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { IncludeUnaltered = includeUnaltered });

                Assert.Equal(FileStatus.ModifiedInIndex, status[file].State);

                Assert.NotNull(status);
                Assert.Equal(6 + unalteredCount, status.Count());
                Assert.True(status.IsDirty);

                Assert.Equal("new_untracked_file.txt", status.Untracked.Select(s => s.FilePath).Single());
                Assert.Equal("modified_unstaged_file.txt", status.Modified.Select(s => s.FilePath).Single());
                Assert.Equal("deleted_unstaged_file.txt", status.Missing.Select(s => s.FilePath).Single());
                Assert.Equal("new_tracked_file.txt", status.Added.Select(s => s.FilePath).Single());
                Assert.Equal(file, status.Staged.Select(s => s.FilePath).Single());
                Assert.Equal("deleted_staged_file.txt", status.Removed.Select(s => s.FilePath).Single());

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, file),
                                   "Tclem's favorite commit message: boom");

                Assert.Equal(FileStatus.ModifiedInIndex | FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(file));

                RepositoryStatus status2 = repo.RetrieveStatus(new StatusOptions() { IncludeUnaltered = includeUnaltered });
                Assert.Equal(FileStatus.ModifiedInIndex | FileStatus.ModifiedInWorkdir, status2[file].State);

                Assert.NotNull(status2);
                Assert.Equal(6 + unalteredCount, status2.Count());
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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "old_name.txt",
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n" +
                    "This is a file with enough data to trigger similarity matching.\r\n");

                Commands.Stage(repo, "old_name.txt");

                File.Move(Path.Combine(repo.Info.WorkingDirectory, "old_name.txt"),
                    Path.Combine(repo.Info.WorkingDirectory, "rename_target.txt"));

                RepositoryStatus status = repo.RetrieveStatus(
                    new StatusOptions()
                    {
                        DetectRenamesInIndex = true,
                        DetectRenamesInWorkDir = true
                    });

                Assert.Equal(FileStatus.NewInIndex | FileStatus.RenamedInWorkdir, status["rename_target.txt"].State);
                Assert.Equal(100, status["rename_target.txt"].IndexToWorkDirRenameDetails.Similarity);
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfRenamedFilesInIndex()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                File.Move(
                    Path.Combine(repo.Info.WorkingDirectory, "1.txt"),
                    Path.Combine(repo.Info.WorkingDirectory, "rename_target.txt"));

                Commands.Stage(repo, "1.txt");
                Commands.Stage(repo, "rename_target.txt");

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

                Commands.Stage(repo, "file.txt");
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
                Assert.Equal(FileStatus.RenamedInWorkdir, status.Single().State);

                Commands.Stage(repo, "file.txt");
                Commands.Stage(repo, "renamed.txt");

                status = repo.RetrieveStatus(opts);

                Assert.Equal(FileStatus.RenamedInIndex, status.Single().State);

                File.Move(Path.Combine(repo.Info.WorkingDirectory, "renamed.txt"),
                    Path.Combine(repo.Info.WorkingDirectory, "renamed_again.txt"));

                status = repo.RetrieveStatus(opts);

                Assert.Equal(FileStatus.RenamedInWorkdir | FileStatus.RenamedInIndex,
                    status.Single().State);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanRetrieveTheStatusOfANewRepository(bool includeUnaltered)
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { IncludeUnaltered = includeUnaltered });
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
        public void RetrievingTheStatusOfARepositoryReturnsGitPaths()
        {
            // Build relative path
            string relFilePath = Path.Combine("directory", "Testfile.txt");

            // Open the repository
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.WorkingDirectory, relFilePath, "Anybody out there?");

                // Add the file to the index
                Commands.Stage(repo, relFilePath);

                // Get the repository status
                RepositoryStatus repoStatus = repo.RetrieveStatus();

                Assert.Equal(1, repoStatus.Count());
                StatusEntry statusEntry = repoStatus.Single();

                Assert.Equal(relFilePath.Replace('\\', '/'), statusEntry.FilePath);

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
            string path = SandboxStandardTestRepo();
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

                relativePath = relativePath.Replace('\\', '/');
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

        [Theory]
        [InlineData(true, FileStatus.Unaltered, FileStatus.Unaltered)]
        [InlineData(false, FileStatus.DeletedFromWorkdir, FileStatus.NewInWorkdir)]
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

                Commands.Stage(repo, lowercasedFilename);
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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "bin/look-ma.txt", "I'm going to be ignored!");
                Touch(repo.Info.WorkingDirectory, "bin/what-about-me.txt", "Huh?");

                const string gitIgnore = ".gitignore";
                Touch(repo.Info.WorkingDirectory, gitIgnore, "bin");

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus("bin/look-ma.txt"));
                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus("bin/what-about-me.txt"));

                RepositoryStatus newStatus = repo.RetrieveStatus();
                Assert.Equal(new[] { "bin/" }, newStatus.Ignored.Select(s => s.FilePath));

                var sb = new StringBuilder();
                sb.AppendLine("bin/*");
                sb.AppendLine("!bin/w*");
                Touch(repo.Info.WorkingDirectory, gitIgnore, sb.ToString());

                Assert.Equal(FileStatus.Ignored, repo.RetrieveStatus("bin/look-ma.txt"));
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus("bin/what-about-me.txt"));

                newStatus = repo.RetrieveStatus();

                Assert.Equal(new[] { "bin/look-ma.txt" }, newStatus.Ignored.Select(s => s.FilePath));
                Assert.True(newStatus.Untracked.Select(s => s.FilePath).Contains("bin/what-about-me.txt"));
            }
        }

        [Fact]
        public void CanRetrieveStatusOfFilesInSubmodule()
        {
            var path = SandboxSubmoduleTestRepo();
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
            var path = SandboxSubmoduleTestRepo();
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
            string path = SandboxStandardTestRepo();
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
            string path = SandboxStandardTestRepo();
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
            string path = SandboxStandardTestRepo();
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

        [Fact]
        public void RetrievingTheStatusHonorsAssumedUnchangedMarkedIndexEntries()
        {
            var path = SandboxAssumeUnchangedTestRepo();
            using (var repo = new Repository(path))
            {
                var status = repo.RetrieveStatus();
                Assert.Equal("hello.txt", status.Modified.Single().FilePath);
            }
        }

        [Fact]
        public void CanIncludeStatusOfUnalteredFiles()
        {
            var path = SandboxStandardTestRepo();
            string[] unalteredPaths = {
                "1.txt",
                "1/branch_file.txt",
                "branch_file.txt",
                "new.txt",
                "README",
            };

            using (var repo = new Repository(path))
            {
                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { IncludeUnaltered = true });

                Assert.Equal(unalteredPaths.Length, status.Unaltered.Count());
                Assert.Equal(unalteredPaths, status.Unaltered.OrderBy(s => s.FilePath).Select(s => s.FilePath).ToArray());
            }
        }

        [Fact]
        public void UnalteredFilesDontMarkIndexAsDirty()
        {
            var path = SandboxStandardTestRepo();

            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                RepositoryStatus status = repo.RetrieveStatus(new StatusOptions() { IncludeUnaltered = true });

                Assert.Equal(false, status.IsDirty);
                Assert.Equal(9, status.Count());
            }
        }
    }
}
