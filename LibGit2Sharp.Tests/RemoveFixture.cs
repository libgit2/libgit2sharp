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
        [InlineData(false, "1/branch_file.txt", false, FileStatus.Unaltered, true, true, FileStatus.Untracked | FileStatus.Removed)]
        [InlineData(true, "1/branch_file.txt", false, FileStatus.Unaltered, true, false, FileStatus.Removed)]
        /***
         * Test case: file exists in the index, and has been removed from the wd.
         *   'git rm <file> and 'git rm --cached <file>' both work (file removed from the index)
         */
        [InlineData(true, "deleted_unstaged_file.txt", false, FileStatus.Missing, false, false, FileStatus.Removed)]
        [InlineData(false, "deleted_unstaged_file.txt", false, FileStatus.Missing, false, false, FileStatus.Removed)]
        /***
         * Test case: modified file in wd, the modifications have not been promoted to the index yet.
         *   'git rm --cached <file>' works (removes the file from the index)
         *   'git rm <file>' fails ("error: '<file>' has local modifications").
         */
        [InlineData(false, "modified_unstaged_file.txt", false, FileStatus.Modified, true, true, FileStatus.Untracked | FileStatus.Removed)]
        [InlineData(true, "modified_unstaged_file.txt", true,  FileStatus.Modified, true, true, 0)]
        /***
         * Test case: modified file in wd, the modifications have already been promoted to the index.
         *   'git rm --cached <file>' works (removes the file from the index)
         *   'git rm <file>' fails ("error: '<file>' has changes staged in the index")
         */
        [InlineData(false, "modified_staged_file.txt", false, FileStatus.Staged, true, true, FileStatus.Untracked | FileStatus.Removed)]
        [InlineData(true, "modified_staged_file.txt", true, FileStatus.Staged, true, true, 0)]
        /***
         * Test case: modified file in wd, the modifications have already been promoted to the index, and
         * the file does not exist in the HEAD.
         *   'git rm --cached <file>' works (removes the file from the index)
         *   'git rm <file>' throws ("error: '<file>' has changes staged in the index")
         */
        [InlineData(false, "new_tracked_file.txt", false, FileStatus.Added, true, true, FileStatus.Untracked)]
        [InlineData(true, "new_tracked_file.txt", true, FileStatus.Added, true, true, 0)]
        public void CanRemoveAnUnalteredFileFromTheIndexWithoutRemovingItFromTheWorkingDirectory(
            bool removeFromWorkdir, string filename, bool throws, FileStatus initialStatus, bool existsBeforeRemove, bool existsAfterRemove, FileStatus lastStatus)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(initialStatus, repo.Index.RetrieveStatus(filename));
                Assert.Equal(existsBeforeRemove, File.Exists(fullpath));

                if (throws)
                {
                    Assert.Throws<RemoveFromIndexException>(() => repo.Index.Remove(filename, removeFromWorkdir));
                    Assert.Equal(count, repo.Index.Count);
                }
                else
                {
                    repo.Index.Remove(filename, removeFromWorkdir);

                    Assert.Equal(count - 1, repo.Index.Count);
                    Assert.Equal(existsAfterRemove, File.Exists(fullpath));
                    Assert.Equal(lastStatus, repo.Index.RetrieveStatus(filename));
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

            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(true, File.Exists(fullpath));

                File.AppendAllText(fullpath, "additional content");
                Assert.Equal(FileStatus.Staged | FileStatus.Modified, repo.Index.RetrieveStatus(filename));

                Assert.Throws<RemoveFromIndexException>(() => repo.Index.Remove(filename));
                Assert.Throws<RemoveFromIndexException>(() => repo.Index.Remove(filename, false));
            }
        }

        [Fact]
        public void CanRemoveAFolderThroughUsageOfPathspecsForNewlyAddedFiles()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Index.Stage(Touch(repo.Info.WorkingDirectory, "2/subdir1/2.txt", "whone"));
                repo.Index.Stage(Touch(repo.Info.WorkingDirectory, "2/subdir1/3.txt", "too"));
                repo.Index.Stage(Touch(repo.Info.WorkingDirectory, "2/subdir2/4.txt", "tree"));
                repo.Index.Stage(Touch(repo.Info.WorkingDirectory, "2/5.txt", "for"));
                repo.Index.Stage(Touch(repo.Info.WorkingDirectory, "2/6.txt", "fyve"));

                int count = repo.Index.Count;

                Assert.True(Directory.Exists(Path.Combine(repo.Info.WorkingDirectory, "2")));
                repo.Index.Remove("2", false);

                Assert.Equal(count - 5, repo.Index.Count);
            }
        }

        [Fact]
        public void CanRemoveAFolderThroughUsageOfPathspecsForFilesAlreadyInTheIndexAndInTheHEAD()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                Assert.True(Directory.Exists(Path.Combine(repo.Info.WorkingDirectory, "1")));
                repo.Index.Remove("1");

                Assert.False(Directory.Exists(Path.Combine(repo.Info.WorkingDirectory, "1")));
                Assert.Equal(count - 1, repo.Index.Count);
            }
        }

        [Theory]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed)]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        public void RemovingAnUnknownFileWithLaxExplicitPathsValidationDoesntThrow(string relativePath, FileStatus status)
        {
            for (int i = 0; i < 2; i++)
            {
                using (var repo = new Repository(StandardTestRepoPath))
                {
                    Assert.Null(repo.Index[relativePath]);
                    Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));

                    repo.Index.Remove(relativePath, i % 2 == 0);
                    repo.Index.Remove(relativePath, i % 2 == 0,
                                      new ExplicitPathsOptions {ShouldFailOnUnmatchedPath = false});
                }
            }
        }

        [Theory]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed)]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        public void RemovingAnUnknownFileThrowsIfExplicitPath(string relativePath, FileStatus status)
        {
            for (int i = 0; i < 2; i++)
            {
                using (var repo = new Repository(StandardTestRepoPath))
                {
                    Assert.Null(repo.Index[relativePath]);
                    Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));

                    Assert.Throws<UnmatchedPathException>(
                        () => repo.Index.Remove(relativePath, i%2 == 0, new ExplicitPathsOptions()));
                }
            }
        }

        [Fact]
        public void RemovingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Remove(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Remove((string)null));
                Assert.Throws<ArgumentException>(() => repo.Index.Remove(new string[] { }));
                Assert.Throws<ArgumentException>(() => repo.Index.Remove(new string[] { null }));
            }
        }
    }
}
