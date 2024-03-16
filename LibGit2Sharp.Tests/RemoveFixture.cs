using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RemoveFixture : BaseFixture
    {
        [Theory]
        /***
         * Test case: file exists in workdir and index, and has not been modified.
         *   'git rm --cached <file>' works (file removed only from index).
         *   'git rm <file>' works (file removed from both index and workdir).
         */
        [InlineData(false, "1/branch_file.txt", false, FileStatus.Unaltered, true, true, FileStatus.NewInWorkdir | FileStatus.DeletedFromIndex)]
        [InlineData(true, "1/branch_file.txt", false, FileStatus.Unaltered, true, false, FileStatus.DeletedFromIndex)]
        /***
         * Test case: file exists in the index, and has been removed from the wd.
         *   'git rm <file> and 'git rm --cached <file>' both work (file removed from the index)
         */
        [InlineData(true, "deleted_unstaged_file.txt", false, FileStatus.DeletedFromWorkdir, false, false, FileStatus.DeletedFromIndex)]
        [InlineData(false, "deleted_unstaged_file.txt", false, FileStatus.DeletedFromWorkdir, false, false, FileStatus.DeletedFromIndex)]
        /***
         * Test case: modified file in wd, the modifications have not been promoted to the index yet.
         *   'git rm --cached <file>' works (removes the file from the index)
         *   'git rm <file>' fails ("error: '<file>' has local modifications").
         */
        [InlineData(false, "modified_unstaged_file.txt", false, FileStatus.ModifiedInWorkdir, true, true, FileStatus.NewInWorkdir | FileStatus.DeletedFromIndex)]
        [InlineData(true, "modified_unstaged_file.txt", true,  FileStatus.ModifiedInWorkdir, true, true, FileStatus.Unaltered)]
        /***
         * Test case: modified file in wd, the modifications have already been promoted to the index.
         *   'git rm --cached <file>' works (removes the file from the index)
         *   'git rm <file>' fails ("error: '<file>' has changes staged in the index")
         */
        [InlineData(false, "modified_staged_file.txt", false, FileStatus.ModifiedInIndex, true, true, FileStatus.NewInWorkdir | FileStatus.DeletedFromIndex)]
        [InlineData(true, "modified_staged_file.txt", true, FileStatus.ModifiedInIndex, true, true, FileStatus.Unaltered)]
        /***
         * Test case: modified file in wd, the modifications have already been promoted to the index, and
         * the file does not exist in the HEAD.
         *   'git rm --cached <file>' works (removes the file from the index)
         *   'git rm <file>' throws ("error: '<file>' has changes staged in the index")
         */
        [InlineData(false, "new_tracked_file.txt", false, FileStatus.NewInIndex, true, true, FileStatus.NewInWorkdir)]
        [InlineData(true, "new_tracked_file.txt", true, FileStatus.NewInIndex, true, true, FileStatus.Unaltered)]
        public void CanRemoveAnUnalteredFileFromTheIndexWithoutRemovingItFromTheWorkingDirectory(
            bool removeFromWorkdir, string filename, bool throws, FileStatus initialStatus, bool existsBeforeRemove, bool existsAfterRemove, FileStatus lastStatus)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(initialStatus, repo.RetrieveStatus(filename));
                Assert.Equal(existsBeforeRemove, File.Exists(fullpath));

                if (throws)
                {
                    Assert.Throws<RemoveFromIndexException>(() => Commands.Remove(repo, filename, removeFromWorkdir));
                    Assert.Equal(count, repo.Index.Count);
                }
                else
                {
                    Commands.Remove(repo, filename, removeFromWorkdir);

                    Assert.Equal(count - 1, repo.Index.Count);
                    Assert.Equal(existsAfterRemove, File.Exists(fullpath));
                    Assert.Equal(lastStatus, repo.RetrieveStatus(filename));
                }
            }
        }

        /***
         * Test case: modified file in wd, the modifications have already been promoted to the index, and
         * new modifications have been made in the wd.
         *   'git rm <file>' and 'git rm --cached <file>' both fail ("error: '<file>' has staged content different from both the file and the HEAD")
         */
        [Fact]
        public void RemovingAModifiedFileWhoseChangesHaveBeenPromotedToTheIndexAndWithAdditionalModificationsMadeToItThrows()
        {
            const string filename = "modified_staged_file.txt";

            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.True(File.Exists(fullpath));

                File.AppendAllText(fullpath, "additional content");
                Assert.Equal(FileStatus.ModifiedInIndex | FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(filename));

                Assert.Throws<RemoveFromIndexException>(() => Commands.Remove(repo, filename));
                Assert.Throws<RemoveFromIndexException>(() => Commands.Remove(repo, filename, false));
            }
        }

        [Fact]
        public void CanRemoveAFolderThroughUsageOfPathspecsForNewlyAddedFiles()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Commands.Stage(repo, Touch(repo.Info.WorkingDirectory, "2/subdir1/2.txt", "whone"));
                Commands.Stage(repo, Touch(repo.Info.WorkingDirectory, "2/subdir1/3.txt", "too"));
                Commands.Stage(repo, Touch(repo.Info.WorkingDirectory, "2/subdir2/4.txt", "tree"));
                Commands.Stage(repo, Touch(repo.Info.WorkingDirectory, "2/5.txt", "for"));
                Commands.Stage(repo, Touch(repo.Info.WorkingDirectory, "2/6.txt", "fyve"));

                int count = repo.Index.Count;

                Assert.True(Directory.Exists(Path.Combine(repo.Info.WorkingDirectory, "2")));
                Commands.Remove(repo, "2", false);

                Assert.Equal(count - 5, repo.Index.Count);
            }
        }

        [Fact]
        public void CanRemoveAFolderThroughUsageOfPathspecsForFilesAlreadyInTheIndexAndInTheHEAD()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                Assert.True(Directory.Exists(Path.Combine(repo.Info.WorkingDirectory, "1")));
                Commands.Remove(repo, "1");

                Assert.False(Directory.Exists(Path.Combine(repo.Info.WorkingDirectory, "1")));
                Assert.Equal(count - 1, repo.Index.Count);
            }
        }

        [Theory]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromIndex)]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        public void RemovingAnUnknownFileWithLaxExplicitPathsValidationDoesntThrow(string relativePath, FileStatus status)
        {
            for (int i = 0; i < 2; i++)
            {
                var path = SandboxStandardTestRepoGitDir();
                using (var repo = new Repository(path))
                {
                    Assert.Null(repo.Index[relativePath]);
                    Assert.Equal(status, repo.RetrieveStatus(relativePath));

                    Commands.Remove(repo, relativePath, i % 2 == 0);
                    Commands.Remove(repo, relativePath, i % 2 == 0,
                                      new ExplicitPathsOptions {ShouldFailOnUnmatchedPath = false});
                }
            }
        }

        [Theory]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromIndex)]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        public void RemovingAnUnknownFileThrowsIfExplicitPath(string relativePath, FileStatus status)
        {
            for (int i = 0; i < 2; i++)
            {
                var path = SandboxStandardTestRepoGitDir();
                using (var repo = new Repository(path))
                {
                    Assert.Null(repo.Index[relativePath]);
                    Assert.Equal(status, repo.RetrieveStatus(relativePath));

                    Assert.Throws<UnmatchedPathException>(
                        () => Commands.Remove(repo, relativePath, i%2 == 0, new ExplicitPathsOptions()));
                }
            }
        }

        [Fact]
        public void RemovingFileWithBadParamsThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => Commands.Remove(repo, string.Empty));
                Assert.Throws<ArgumentNullException>(() => Commands.Remove(repo, (string)null));
                Assert.Throws<ArgumentException>(() => Commands.Remove(repo, Array.Empty<string>()));
                Assert.Throws<ArgumentNullException>(() => Commands.Remove(repo, new string[] { null }));
            }
        }
    }
}
