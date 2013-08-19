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
                FileStatus status = repo.Index.RetrieveStatus("new_tracked_file.txt");
                Assert.Equal(FileStatus.Added, status);
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

                FileStatus status = repo.Index.RetrieveStatus(filePath);
                Assert.Equal(FileStatus.Untracked, status);
            }
        }

        [Fact]
        public void RetrievingTheStatusOfADirectoryThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<AmbiguousSpecificationException>(() => { FileStatus status = repo.Index.RetrieveStatus("1"); });
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfTheWholeWorkingDirectory()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string file = "modified_staged_file.txt";

                RepositoryStatus status = repo.Index.RetrieveStatus();

                Assert.Equal(FileStatus.Staged, status[file]);

                Assert.NotNull(status);
                Assert.Equal(6, status.Count());
                Assert.True(status.IsDirty);

                Assert.Equal("new_untracked_file.txt", status.Untracked.Single());
                Assert.Equal("modified_unstaged_file.txt", status.Modified.Single());
                Assert.Equal("deleted_unstaged_file.txt", status.Missing.Single());
                Assert.Equal("new_tracked_file.txt", status.Added.Single());
                Assert.Equal(file, status.Staged.Single());
                Assert.Equal("deleted_staged_file.txt", status.Removed.Single());

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, file),
                                   "Tclem's favorite commit message: boom");

                Assert.Equal(FileStatus.Staged | FileStatus.Modified, repo.Index.RetrieveStatus(file));

                RepositoryStatus status2 = repo.Index.RetrieveStatus();
                Assert.Equal(FileStatus.Staged | FileStatus.Modified, status2[file]);

                Assert.NotNull(status2);
                Assert.Equal(6, status2.Count());
                Assert.True(status2.IsDirty);

                Assert.Equal("new_untracked_file.txt", status2.Untracked.Single());
                Assert.Equal(new[] { file, "modified_unstaged_file.txt" }, status2.Modified);
                Assert.Equal("deleted_unstaged_file.txt", status2.Missing.Single());
                Assert.Equal("new_tracked_file.txt", status2.Added.Single());
                Assert.Equal(file, status2.Staged.Single());
                Assert.Equal("deleted_staged_file.txt", status2.Removed.Single());
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfANewRepository()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                RepositoryStatus status = repo.Index.RetrieveStatus();
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
                repo.Index.Stage(relFilePath);

                // Get the repository status
                RepositoryStatus repoStatus = repo.Index.RetrieveStatus();

                Assert.Equal(1, repoStatus.Count());
                StatusEntry statusEntry = repoStatus.Single();

                Assert.Equal(relFilePath, statusEntry.FilePath);

                Assert.Equal(statusEntry.FilePath, repoStatus.Added.Single());
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

                RepositoryStatus status = repo.Index.RetrieveStatus();
                Assert.Equal(new[] { relativePath }, status.Untracked);

                Touch(repo.Info.WorkingDirectory, ".gitignore", "*.txt" + Environment.NewLine);

                RepositoryStatus newStatus = repo.Index.RetrieveStatus();
                Assert.Equal(".gitignore", newStatus.Untracked.Single());

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(relativePath));
                Assert.Equal(new[] { relativePath }, newStatus.Ignored);
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

                RepositoryStatus status = repo.Index.RetrieveStatus();

                Assert.Equal(new[]{relativePath, "new_untracked_file.txt"}, status.Untracked);

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

                RepositoryStatus newStatus = repo.Index.RetrieveStatus();
                Assert.Equal(".gitignore", newStatus.Untracked.Single());

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(relativePath));
                Assert.Equal(new[] { relativePath, "new_untracked_file.txt" }, newStatus.Ignored);
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

                Assert.Throws<AmbiguousSpecificationException>(() => repo.Index.RetrieveStatus(relativePath));
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

                repo.Index.Stage(lowercasedFilename);
                repo.Commit("initial", Constants.Signature, Constants.Signature);
            }

            using (var repo = new Repository(repoPath))
            {
                const string upercasedFilename = "Plop";

                string camelCasedPath = Path.Combine(repo.Info.WorkingDirectory, upercasedFilename);
                File.Move(lowerCasedPath, camelCasedPath);

                Assert.Equal(expectedlowerCasedFileStatus, repo.Index.RetrieveStatus(lowercasedFilename));
                Assert.Equal(expectedCamelCasedFileStatus, repo.Index.RetrieveStatus(upercasedFilename));

                AssertStatus(shouldIgnoreCase, expectedlowerCasedFileStatus, repo, camelCasedPath.ToLowerInvariant());
                AssertStatus(shouldIgnoreCase, expectedCamelCasedFileStatus, repo, camelCasedPath.ToUpperInvariant());
            }
        }

        private static void AssertStatus(bool shouldIgnoreCase, FileStatus expectedFileStatus, IRepository repo, string path)
        {
            try
            {
                Assert.Equal(expectedFileStatus, repo.Index.RetrieveStatus(path));
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

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus("bin/look-ma.txt"));
                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus("bin/what-about-me.txt"));

                RepositoryStatus newStatus = repo.Index.RetrieveStatus();
                Assert.Equal(new[] { "bin" + dirSep }, newStatus.Ignored);

                var sb = new StringBuilder();
                sb.AppendLine("bin/*");
                sb.AppendLine("!bin/w*");
                Touch(repo.Info.WorkingDirectory, gitIgnore, sb.ToString());

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus("bin/look-ma.txt"));
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus("bin/what-about-me.txt"));

                newStatus = repo.Index.RetrieveStatus();

                Assert.Equal(new[] { "bin" + dirSep + "look-ma.txt" }, newStatus.Ignored);
                Assert.True(newStatus.Untracked.Contains("bin" + dirSep + "what-about-me.txt" ));
            }
        }
    }
}
