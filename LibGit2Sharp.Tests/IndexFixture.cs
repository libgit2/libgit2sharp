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
                Assert.Equal(expectedEntries, repo.Index.Select(e => e.Path).ToArray());
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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;
                const string filename = "new_tracked_file.txt";
                IndexEntry blob = repo.Index[filename];

                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));

                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "brand new content");
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
        public void StagingAnUnknownFileThrows(string relativePath, FileStatus status)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Index[relativePath]);
                Assert.Equal(status, repo.Index.RetrieveStatus(relativePath));

                Assert.Throws<LibGit2SharpException>(() => repo.Index.Stage(relativePath));
            }
        }

        [Fact]
        public void CanStageTheRemovalOfAStagedFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
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

        [Fact]
        public void StagingANewVersionOfAFileThenUnstagingItRevertsTheBlobToTheVersionOfHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                string filename = Path.Combine("1", "branch_file.txt");
                const string posixifiedFileName = "1/branch_file.txt";
                ObjectId blobId = repo.Index[posixifiedFileName].Id;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                File.AppendAllText(fullpath, "Is there there anybody out there?");
                repo.Index.Stage(filename);

                Assert.Equal(count, repo.Index.Count);
                Assert.NotEqual((blobId), repo.Index[posixifiedFileName].Id);

                repo.Index.Unstage(posixifiedFileName);

                Assert.Equal(count, repo.Index.Count);
                Assert.Equal(blobId, repo.Index[posixifiedFileName].Id);
            }
        }

        [Fact]
        public void CanStageANewFileInAPersistentManner()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string filename = "unit_test.txt";
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "some contents");
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus(filename));
                Assert.Null(repo.Index[filename]);

                repo.Index.Stage(filename);
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));
                Assert.Equal(FileStatus.Added, repo.Index[filename].State);
            }

            using (var repo = new Repository(path.RepositoryPath))
            {
                const string filename = "unit_test.txt";
                Assert.NotNull(repo.Index[filename]);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus(filename));
                Assert.Equal(FileStatus.Added, repo.Index[filename].State);
            }
        }

        [Fact]
        public void CanStageANewFileWithAFullPath()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                const string filename = "new_untracked_file.txt";
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                Assert.True(File.Exists(fullPath));

                repo.Index.Stage(fullPath);

                Assert.Equal(count + 1, repo.Index.Count);
                Assert.NotNull(repo.Index[filename]);
            }
        }

        [Fact]
        public void CanStageANewFileWithARelativePathContainingNativeDirectorySeparatorCharacters()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                DirectoryInfo di = Directory.CreateDirectory(Path.Combine(repo.Info.WorkingDirectory, "Project"));
                string file = Path.Combine("Project", "a_file.txt");

                File.WriteAllText(Path.Combine(di.FullName, "a_file.txt"), "With backward slash on Windows!");

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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Path.Combine(di.FullName, filename);
                File.WriteAllText(fullPath, "some contents");

                Assert.Throws<ArgumentException>(() => repo.Index.Stage(fullPath));
            }
        }

        [Fact]
        public void StageFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage((string)null));
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(new string[] { }));
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(new string[] { null }));
            }
        }

        [Theory]
        [InlineData("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [InlineData("deleted_unstaged_file.txt", FileStatus.Missing, true, FileStatus.Missing, true, 0)]
        [InlineData("modified_unstaged_file.txt", FileStatus.Modified, true, FileStatus.Modified, true, 0)]
        [InlineData("new_untracked_file.txt", FileStatus.Untracked, false, FileStatus.Untracked, false, 0)]
        [InlineData("modified_staged_file.txt", FileStatus.Staged, true, FileStatus.Modified, true, 0)]
        [InlineData("new_tracked_file.txt", FileStatus.Added, true, FileStatus.Untracked, false, -1)]
        public void CanUnStage(string relativePath, FileStatus currentStatus, bool doesCurrentlyExistInTheIndex, FileStatus expectedStatusOnceStaged, bool doesExistInTheIndexOnceStaged, int expectedIndexCountVariation)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;
                Assert.Equal(doesCurrentlyExistInTheIndex, (repo.Index[relativePath] != null));
                Assert.Equal(currentStatus, repo.Index.RetrieveStatus(relativePath));

                repo.Index.Unstage(relativePath);

                Assert.Equal(count + expectedIndexCountVariation, repo.Index.Count);
                Assert.Equal(doesExistInTheIndexOnceStaged, (repo.Index[relativePath] != null));
                Assert.Equal(expectedStatusOnceStaged, repo.Index.RetrieveStatus(relativePath));
            }
        }

        [Fact]
        public void CanUnstageTheRemovalOfAFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                const string filename = "deleted_staged_file.txt";

                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                Assert.False(File.Exists(fullPath));

                Assert.Equal(FileStatus.Removed, repo.Index.RetrieveStatus(filename));

                repo.Index.Unstage(filename);
                Assert.Equal(count + 1, repo.Index.Count);

                Assert.Equal(FileStatus.Missing, repo.Index.RetrieveStatus(filename));
            }
        }

        [Fact]
        public void UnstagingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Unstage((string)null));
                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(new string[] { }));
                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(new string[] { null }));
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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
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

        [Theory]
        [InlineData("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Removed)]
        [InlineData("deleted_unstaged_file.txt", FileStatus.Missing, false, FileStatus.Removed)]
        public void CanRemoveAFile(string filename, FileStatus initialStatus, bool shouldInitiallyExist, FileStatus finalStatus)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
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
        [InlineData("deleted_staged_file.txt")]
        [InlineData("modified_unstaged_file.txt")]
        [InlineData("shadowcopy_of_an_unseen_ghost.txt")]
        public void RemovingAInvalidFileThrows(string filepath)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Index.Remove(filepath));
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
