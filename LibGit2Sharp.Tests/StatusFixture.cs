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
                status.ShouldEqual(FileStatus.Added);
            }
        }

        [Fact]
        public void RetrievingTheStatusOfADirectoryThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => { FileStatus status = repo.Index.RetrieveStatus("1"); });
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
                indexEntry.State.ShouldEqual(FileStatus.Staged);

                status.ShouldNotBeNull();
                status.Count().ShouldEqual(6);
                status.IsDirty.ShouldBeTrue();

                status.Untracked.Single().ShouldEqual("new_untracked_file.txt");
                status.Modified.Single().ShouldEqual("modified_unstaged_file.txt");
                status.Missing.Single().ShouldEqual("deleted_unstaged_file.txt");
                status.Added.Single().ShouldEqual("new_tracked_file.txt");
                status.Staged.Single().ShouldEqual(file);
                status.Removed.Single().ShouldEqual("deleted_staged_file.txt");

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, file),
                                   "Tclem's favorite commit message: boom");

                indexEntry.State.ShouldEqual(FileStatus.Staged | FileStatus.Modified);

                RepositoryStatus status2 = repo.Index.RetrieveStatus();

                status2.ShouldNotBeNull();
                status2.Count().ShouldEqual(6);
                status2.IsDirty.ShouldBeTrue();

                status2.Untracked.Single().ShouldEqual("new_untracked_file.txt");
                Assert.Equal(new[] { file, "modified_unstaged_file.txt" }, status2.Modified);
                status2.Missing.Single().ShouldEqual("deleted_unstaged_file.txt");
                status2.Added.Single().ShouldEqual("new_tracked_file.txt");
                status2.Staged.Single().ShouldEqual(file);
                status2.Removed.Single().ShouldEqual("deleted_staged_file.txt");
            }
        }

        [Fact]
        public void CanRetrieveTheStatusOfANewRepository()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (Repository repo = Repository.Init(scd.DirectoryPath))
            {
                RepositoryStatus status = repo.Index.RetrieveStatus();
                status.ShouldNotBeNull();
                status.Count().ShouldEqual(0);
                status.IsDirty.ShouldBeFalse();

                status.Untracked.Count().ShouldEqual(0);
                status.Modified.Count().ShouldEqual(0);
                status.Missing.Count().ShouldEqual(0);
                status.Added.Count().ShouldEqual(0);
                status.Staged.Count().ShouldEqual(0);
                status.Removed.Count().ShouldEqual(0);
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

                repoStatus.Count().ShouldEqual(1);
                StatusEntry statusEntry = repoStatus.Single();

                string expectedPath = string.Format("{0}{1}{2}", directoryName, Path.DirectorySeparatorChar, fileName);
                statusEntry.FilePath.ShouldEqual(expectedPath);

                repoStatus.Added.Single().ShouldEqual(statusEntry.FilePath);
            }
        }

        [Fact]
        public void RetrievingTheStatusOfAnEmptyRepositoryHonorsTheGitIgnoreDirectives()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (Repository repo = Repository.Init(scd.DirectoryPath))
            {
                string relativePath = "look-ma.txt";
                string fullFilePath = Path.Combine(repo.Info.WorkingDirectory, relativePath);
                File.WriteAllText(fullFilePath, "I'm going to be ignored!");

                RepositoryStatus status = repo.Index.RetrieveStatus();
                Assert.Equal(new[] { relativePath }, status.Untracked);

                string gitignorePath = Path.Combine(repo.Info.WorkingDirectory, ".gitignore");
                File.WriteAllText(gitignorePath, "*.txt" + Environment.NewLine);

                RepositoryStatus newStatus = repo.Index.RetrieveStatus();
                newStatus.Untracked.Single().ShouldEqual(".gitignore");

                repo.Index.RetrieveStatus(relativePath).ShouldEqual(FileStatus.Ignored);
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
                newStatus.Untracked.Single().ShouldEqual(".gitignore");

                repo.Index.RetrieveStatus(relativePath).ShouldEqual(FileStatus.Ignored);
                Assert.Equal(new[] { relativePath, "new_untracked_file.txt" }, newStatus.Ignored);
            }
        }
    }
}
