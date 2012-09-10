using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

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

        [Fact]
        public void RetrievingTheStatusOfADirectoryThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<AmbiguousException>(() => { FileStatus status = repo.Index.RetrieveStatus("1"); });
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfTheWholeWorkingDirectory()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string file = "modified_staged_file.txt";

                RepositoryStatus status = repo.Index.RetrieveStatus();

                IndexEntry indexEntry = repo.Index[file];
                Assert.Equal(FileStatus.Staged, indexEntry.State);

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

                Assert.Equal(FileStatus.Staged | FileStatus.Modified, indexEntry.State);

                RepositoryStatus status2 = repo.Index.RetrieveStatus();

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
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (Repository repo = Repository.Init(scd.DirectoryPath))
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
            // Initialize a new repository
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            const string directoryName = "directory";
            const string fileName = "Testfile.txt";

            // Create a file and insert some content
            string directoryPath = Path.Combine(scd.RootedDirectoryPath, directoryName);
            string filePath = Path.Combine(directoryPath, fileName);

            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, "Anybody out there?");

            // Open the repository
            using (Repository repo = Repository.Init(scd.DirectoryPath))
            {
                // Add the file to the index
                repo.Index.Stage(filePath);

                // Get the repository status
                RepositoryStatus repoStatus = repo.Index.RetrieveStatus();

                Assert.Equal(1, repoStatus.Count());
                StatusEntry statusEntry = repoStatus.Single();

                Assert.Equal(Path.Combine(directoryName, fileName), statusEntry.FilePath);

                Assert.Equal(statusEntry.FilePath, repoStatus.Added.Single());
            }
        }

        [Fact]
        public void RetrievingTheStatusOfAnEmptyRepositoryHonorsTheGitIgnoreDirectives()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (Repository repo = Repository.Init(scd.DirectoryPath))
            {
                const string relativePath = "look-ma.txt";
                string fullFilePath = Path.Combine(repo.Info.WorkingDirectory, relativePath);
                File.WriteAllText(fullFilePath, "I'm going to be ignored!");

                RepositoryStatus status = repo.Index.RetrieveStatus();
                Assert.Equal(new[] { relativePath }, status.Untracked);

                string gitignorePath = Path.Combine(repo.Info.WorkingDirectory, ".gitignore");
                File.WriteAllText(gitignorePath, "*.txt" + Environment.NewLine);

                RepositoryStatus newStatus = repo.Index.RetrieveStatus();
                Assert.Equal(".gitignore", newStatus.Untracked.Single());

                Assert.Equal(FileStatus.Ignored, repo.Index.RetrieveStatus(relativePath));
                Assert.Equal(new[] { relativePath }, newStatus.Ignored);
            }
        }

        [Fact]
        public void RetrievingTheStatusOfTheRepositoryHonorsTheGitIgnoreDirectives()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                string relativePath = Path.Combine("1", "look-ma.txt");
                string fullFilePath = Path.Combine(repo.Info.WorkingDirectory, relativePath);
                File.WriteAllText(fullFilePath, "I'm going to be ignored!");

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

                string gitignorePath = Path.Combine(repo.Info.WorkingDirectory, ".gitignore");
                File.WriteAllText(gitignorePath, "*.txt" + Environment.NewLine);

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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                string relativePath = Path.Combine("1", "ambiguous1.txt");
                string fullFilePath = Path.Combine(repo.Info.WorkingDirectory, relativePath);
                File.WriteAllText(fullFilePath, "I don't like brackets.");

                relativePath = Path.Combine("1", "ambiguous[1].txt");
                fullFilePath = Path.Combine(repo.Info.WorkingDirectory, relativePath);
                File.WriteAllText(fullFilePath, "Brackets all the way.");

                Assert.Throws<AmbiguousException>(() => repo.Index.RetrieveStatus(relativePath));
            }
        }
    }
}
