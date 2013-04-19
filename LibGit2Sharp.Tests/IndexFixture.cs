using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class IndexFixture : BaseFixture
    {
        private static readonly string subBranchFile = Path.Combine("1", "branch_file.txt");
        private readonly string[] expectedEntries = new[]
                                                        {
                                                            "1.txt",
                                                            subBranchFile,
                                                            "README",
                                                            "branch_file.txt",
                                                            //"deleted_staged_file.txt",
                                                            "deleted_unstaged_file.txt",
                                                            "modified_staged_file.txt",
                                                            "modified_unstaged_file.txt",
                                                            "new.txt",
                                                            "new_tracked_file.txt"
                                                        };

        [Fact]
        public void CanCountEntriesInIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(expectedEntries.Count(), repo.Index.Count);
            }
        }

        [Fact]
        public void CanEnumerateIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(expectedEntries,
                    repo.Index.Select(e => e.Path).OrderBy(p => p, StringComparer.Ordinal).ToArray());
            }
        }

        [Fact]
        public void CanFetchAnIndexEntryByItsName()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                IndexEntry entry = repo.Index["README"];
                Assert.Equal("README", entry.Path);

                // Expressed in Posix format...
                IndexEntry entryWithPath = repo.Index["1/branch_file.txt"];
                Assert.Equal(subBranchFile, entryWithPath.Path);

                //...or in native format
                IndexEntry entryWithPath2 = repo.Index[subBranchFile];
                Assert.Equal(entryWithPath, entryWithPath2);
            }
        }

        [Fact]
        public void FetchingAnUnknownIndexEntryReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                IndexEntry entry = repo.Index["I-do-not-exist.txt"];
                Assert.Null(entry);
            }
        }

        [Fact]
        public void ReadIndexWithBadParamsFails()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { IndexEntry entry = repo.Index[null]; });
                Assert.Throws<ArgumentException>(() => { IndexEntry entry = repo.Index[string.Empty]; });
            }
        }

        [Fact]
        public void CanRenameAFile()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                Assert.Equal(0, repo.Index.Count);

                const string oldName = "polite.txt";
                string oldPath = Path.Combine(repo.Info.WorkingDirectory, oldName);

                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus(oldName));

                File.WriteAllText(oldPath, "hello test file\n", Encoding.ASCII);
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(oldName));

                repo.Index.Stage(oldName);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(oldName));

                // Generated through
                // $ echo "hello test file" | git hash-object --stdin
                const string expectedHash = "88df547706c30fa19f02f43cb2396e8129acfd9b";
                Assert.Equal(expectedHash, repo.Index[oldName].Id.Sha);

                Assert.Equal(1, repo.Index.Count);

                Signature who = Constants.Signature;
                repo.Commit("Initial commit", who, who);

                Assert.Equal(FileStatus.Unaltered, repo.Index.RetrieveStatus(oldName));

                const string newName = "being.frakking.polite.txt";

                repo.Index.Move(oldName, newName);
                Assert.Equal(FileStatus.Removed, repo.Index.RetrieveStatus(oldName));
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(newName));

                Assert.Equal(1, repo.Index.Count);
                Assert.Equal(expectedHash, repo.Index[newName].Id.Sha);

                who = who.TimeShift(TimeSpan.FromMinutes(5));
                Commit commit = repo.Commit("Fix file name", who, who);

                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus(oldName));
                Assert.Equal(FileStatus.Unaltered, repo.Index.RetrieveStatus(newName));

                Assert.Equal(expectedHash, commit.Tree[newName].Target.Id.Sha);
            }
        }

        [Theory]
        [InlineData("README", FileStatus.Unaltered, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Removed, FileStatus.Staged)]
        [InlineData("new_tracked_file.txt", FileStatus.Added, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Nonexistent, FileStatus.Staged)]
        [InlineData("modified_staged_file.txt", FileStatus.Staged, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Removed, FileStatus.Staged)]
        [InlineData("modified_unstaged_file.txt", FileStatus.Modified, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Removed, FileStatus.Staged)]
        public void CanMoveAnExistingFileOverANonExistingFile(string sourcePath, FileStatus sourceStatus, string destPath, FileStatus destStatus, FileStatus sourcePostStatus, FileStatus destPostStatus)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(sourceStatus, repo.Index.RetrieveStatus(sourcePath));
                Assert.Equal(destStatus, repo.Index.RetrieveStatus(destPath));

                repo.Index.Move(sourcePath, destPath);

                Assert.Equal(sourcePostStatus, repo.Index.RetrieveStatus(sourcePath));
                Assert.Equal(destPostStatus, repo.Index.RetrieveStatus(destPath));
            }
        }

        [Theory]
        [InlineData("README", FileStatus.Unaltered, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [InlineData("new_tracked_file.txt", FileStatus.Added, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [InlineData("modified_staged_file.txt", FileStatus.Staged, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [InlineData("modified_unstaged_file.txt", FileStatus.Modified, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        public void MovingOverAnExistingFileThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        public void MovingAFileWichIsNotUnderSourceControlThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        [Theory]
        [InlineData("deleted_unstaged_file.txt", FileStatus.Missing, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        [InlineData("i_dont_exist.txt", FileStatus.Nonexistent, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        public void MovingAFileNotInTheWorkingDirectoryThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        private static void InvalidMoveUseCases(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(sourceStatus, repo.Index.RetrieveStatus(sourcePath));

                foreach (var destPath in destPaths)
                {
                    string path = destPath;
                    Assert.Throws<LibGit2SharpException>(() => repo.Index.Move(sourcePath, path));
                }
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

        [Fact]
        public void CanRemoveAnUnalteredFileFromTheIndexWithoutRemovingItFromTheWorkingDirectory()
        {
            const string fileName = "1/branch_file.txt";

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, fileName);

                Assert.Equal(FileStatus.Unaltered, repo.Index.RetrieveStatus(fileName));
                Assert.True(File.Exists(fullpath));

                repo.Index.Remove(fileName, false);

                Assert.Equal(count - 1, repo.Index.Count);
                Assert.True(File.Exists(fullpath));
                Assert.Equal(FileStatus.Untracked | FileStatus.Removed, repo.Index.RetrieveStatus(fileName));
            }
        }

        [Theory]
        [InlineData("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Removed)]
        [InlineData("deleted_unstaged_file.txt", FileStatus.Missing, false, FileStatus.Removed)]
        public void CanRemoveAFile(string filename, FileStatus initialStatus, bool shouldInitiallyExist, FileStatus finalStatus)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(shouldInitiallyExist, File.Exists(fullpath));
                Assert.Equal(initialStatus, repo.Index.RetrieveStatus(filename));

                repo.Index.Remove(filename);

                Assert.Equal(count - 1, repo.Index.Count);
                Assert.False(File.Exists(fullpath));
                Assert.Equal(finalStatus, repo.Index.RetrieveStatus(filename));
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

        /* Test case: modified file in wd, the modifications have not been promoted to the index yet.
         * 'git rm <file>' fails ("error: '<file>' has local modifications").
         */
        [Fact]
        public void RemovingAModifiedFileWhoseChangesHaveNotBeenPromotedToTheIndexThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<RemoveFromIndexException>(() => repo.Index.Remove("modified_unstaged_file.txt"));
            }
        }

        /* Test case: modified file in wd, the modifications have not been promoted to the index yet.
         * 'git rm --cached <file>' works (removes the file from the index)
         */
        [Fact]
        public void CanRemoveAModifiedFileWhoseChangesHaveNotBeenPromotedToTheIndex()
        {
            const string filename = "modified_unstaged_file.txt";

            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(true, File.Exists(fullpath));
                Assert.Equal(FileStatus.Modified, repo.Index.RetrieveStatus(filename));

                repo.Index.Remove(filename, false);

                Assert.Equal(count - 1, repo.Index.Count);
                Assert.True(File.Exists(fullpath));
                Assert.Equal(FileStatus.Untracked | FileStatus.Removed, repo.Index.RetrieveStatus(filename));
            }
        }

        /* Test case: modified file in wd, the modifications have already been promoted to the index.
         * 'git rm <file>' fails ("error: '<file>' has changes staged in the index")
         */
        [Fact]
        public void RemovingAModifiedFileWhoseChangesHaveBeenPromotedToTheIndexThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<RemoveFromIndexException>(() => repo.Index.Remove("modified_staged_file.txt"));
            }
        }

        /* Test case: modified file in wd, the modifications have already been promoted to the index.
         * 'git rm --cached <file>' works (removes the file from the index)
         */
        [Fact]
        public void CanRemoveAModifiedFileWhoseChangesHaveBeenPromotedToTheIndex()
        {
            const string filename = "modified_staged_file.txt";

            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(true, File.Exists(fullpath));
                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus(filename));

                repo.Index.Remove(filename, false);

                Assert.Equal(count - 1, repo.Index.Count);
                Assert.True(File.Exists(fullpath));
                Assert.Equal(FileStatus.Untracked | FileStatus.Removed, repo.Index.RetrieveStatus(filename));
            }
        }

        /* Test case: modified file in wd, the modifications have already been promoted to the index, and
         * new modifications have been made in the wd.
         * 'git rm <file>' and 'git rm --cached <file>' both fail ("error: '<file>' has staged content different from both the file and the HEAD")
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

        /* Test case: modified file in wd, the modifications have already been promoted to the index, and
         * the file does not exist in the HEAD.
         * 'git rm <file>' throws ("error: '<file>' has changes staged in the index")
         */
        [Fact]
        public void RemovingANewlyAddedFileThrows()
        {
            const string filename = "new_tracked_file.txt";

            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(true, File.Exists(fullpath));
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));

                Assert.Throws<RemoveFromIndexException>(() => repo.Index.Remove(filename));
            }
        }

        /* Test case: modified file in wd, the modifications have already been promoted to the index, and
         * the file does not exist in the HEAD.
         * 'git rm --cached <file>' works (removes the file from the index)
         */
        [Fact]
        public void CanRemoveANewlyAddedFile()
        {
            const string filename = "new_tracked_file.txt";

            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(true, File.Exists(fullpath));
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));

                repo.Index.Remove(filename, false);

                Assert.Equal(count - 1, repo.Index.Count);
                Assert.True(File.Exists(fullpath));
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(filename));
            }
        }

        /* Test case: file exists in the index, and has been removed from the wd.
         * 'git rm <file> and 'git rm --cached <file>' both work (remove the file from the index)
         */
        [Fact]
        public void CanRemoveAFileAlreadyDeletedFromTheWorkdir()
        {
            const string filename = "deleted_unstaged_file.txt";

            for (int i = 0; i < 2; i++)
            {
                var path = CloneStandardTestRepo();
                using (var repo = new Repository(path))
                {
                    int count = repo.Index.Count;

                    Assert.Equal(FileStatus.Missing, repo.Index.RetrieveStatus(filename));

                    repo.Index.Remove(filename, i % 2 == 0);

                    Assert.Equal(count - 1, repo.Index.Count);
                    Assert.Equal(FileStatus.Removed, repo.Index.RetrieveStatus(filename));
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

        [Fact]
        public void PathsOfIndexEntriesAreExpressedInNativeFormat()
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

            // Initialize the repository
            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                // Stage the file
                repo.Index.Stage(filePath);

                // Get the index
                Index index = repo.Index;

                // Build relative path
                string relFilePath = Path.Combine(directoryName, fileName);

                // Get the index entry
                IndexEntry ie = index[relFilePath];

                // Make sure the IndexEntry has been found
                Assert.NotNull(ie);

                // Make sure that the (native) relFilePath and ie.Path are equal
                Assert.Equal(relFilePath, ie.Path);
            }
        }

        [Fact]
        public void CanReadIndexEntryAttributes()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(Mode.NonExecutableFile, repo.Index["README"].Mode);
                Assert.Equal(Mode.ExecutableFile, repo.Index["1/branch_file.txt"].Mode);
            }
        }
    }
}
