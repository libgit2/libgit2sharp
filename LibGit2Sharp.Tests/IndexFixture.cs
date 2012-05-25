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
                repo.Index.Count.ShouldEqual(expectedEntries.Count());
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
                entry.Path.ShouldEqual("README");

                // Expressed in Posix format...
                IndexEntry entryWithPath = repo.Index["1/branch_file.txt"];
                entryWithPath.Path.ShouldEqual(subBranchFile);

                //...or in native format
                IndexEntry entryWithPath2 = repo.Index[subBranchFile];
                entryWithPath2.ShouldEqual(entryWithPath);
            }
        }

        [Fact]
        public void FetchingAnUnknownIndexEntryReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                IndexEntry entry = repo.Index["I-do-not-exist.txt"];
                entry.ShouldBeNull();
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
                (repo.Index[relativePath] != null).ShouldEqual(doesCurrentlyExistInTheIndex);
                repo.Index.RetrieveStatus(relativePath).ShouldEqual(currentStatus);

                repo.Index.Stage(relativePath);

                repo.Index.Count.ShouldEqual(count + expectedIndexCountVariation);
                (repo.Index[relativePath] != null).ShouldEqual(doesExistInTheIndexOnceStaged);
                repo.Index.RetrieveStatus(relativePath).ShouldEqual(expectedStatusOnceStaged);
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

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added);

                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "brand new content");
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added | FileStatus.Modified);

                repo.Index.Stage(filename);
                IndexEntry newBlob = repo.Index[filename];

                repo.Index.Count.ShouldEqual(count);
                blob.Id.ShouldNotEqual(newBlob.Id);
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added);
            }
        }

        [Theory]
        [InlineData("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [InlineData("deleted_staged_file.txt", FileStatus.Removed)]
        public void StagingAnUnknownFileThrows(string relativePath, FileStatus status)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                repo.Index[relativePath].ShouldBeNull();
                repo.Index.RetrieveStatus(relativePath).ShouldEqual(status);

                Assert.Throws<LibGit2Exception>(() => repo.Index.Stage(relativePath));
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
                repo.Index[filename].ShouldNotBeNull();

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added);

                File.Delete(Path.Combine(repo.Info.WorkingDirectory, filename));
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added | FileStatus.Missing);

                repo.Index.Stage(filename);
                repo.Index[filename].ShouldBeNull();

                repo.Index.Count.ShouldEqual(count - 1);
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Nonexistent);
            }
        }

        [Fact]
        public void StagingANewVersionOfAFileThenUnstagingItRevertsTheBlobToTheVersionOfHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                string filename = "1" + Path.DirectorySeparatorChar + "branch_file.txt";
                const string posixifiedFileName = "1/branch_file.txt";
                ObjectId blobId = repo.Index[posixifiedFileName].Id;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                File.AppendAllText(fullpath, "Is there there anybody out there?");
                repo.Index.Stage(filename);

                repo.Index.Count.ShouldEqual(count);
                repo.Index[posixifiedFileName].Id.ShouldNotEqual((blobId));

                repo.Index.Unstage(posixifiedFileName);
                repo.Index.Count.ShouldEqual(count);
                repo.Index[posixifiedFileName].Id.ShouldEqual((blobId));
            }
        }

        [Fact]
        public void CanStageANewFileInAPersistentManner()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string filename = "unit_test.txt";
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Nonexistent);
                repo.Index[filename].ShouldBeNull();

                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "some contents");
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Untracked);
                repo.Index[filename].ShouldBeNull();

                repo.Index.Stage(filename);
                repo.Index[filename].ShouldNotBeNull();
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added);
                repo.Index[filename].State.ShouldEqual(FileStatus.Added);
            }

            using (var repo = new Repository(path.RepositoryPath))
            {
                const string filename = "unit_test.txt";
                repo.Index[filename].ShouldNotBeNull();
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added);
                repo.Index[filename].State.ShouldEqual(FileStatus.Added);
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
                File.Exists(fullPath).ShouldBeTrue();

                repo.Index.Stage(fullPath);

                repo.Index.Count.ShouldEqual(count + 1);
                repo.Index[filename].ShouldNotBeNull();
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
                string file = "Project" + Path.DirectorySeparatorChar + "a_file.txt";

                File.WriteAllText(Path.Combine(di.FullName, "a_file.txt"), "With backward slash on Windows!");

                repo.Index.Stage(file);

                repo.Index.Count.ShouldEqual(count + 1);

                const string posixifiedPath = "Project/a_file.txt";
                repo.Index[posixifiedPath].ShouldNotBeNull();
                repo.Index[posixifiedPath].Path.ShouldEqual(file);
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
                (repo.Index[relativePath] != null).ShouldEqual(doesCurrentlyExistInTheIndex);
                repo.Index.RetrieveStatus(relativePath).ShouldEqual(currentStatus);

                repo.Index.Unstage(relativePath);

                repo.Index.Count.ShouldEqual(count + expectedIndexCountVariation);
                (repo.Index[relativePath] != null).ShouldEqual(doesExistInTheIndexOnceStaged);
                repo.Index.RetrieveStatus(relativePath).ShouldEqual(expectedStatusOnceStaged);
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

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Removed);

                repo.Index.Unstage(filename);
                repo.Index.Count.ShouldEqual(count + 1);

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Missing);
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
                repo.Index.Count.ShouldEqual(0);

                const string oldName = "polite.txt";
                string oldPath = Path.Combine(repo.Info.WorkingDirectory, oldName);

                repo.Index.RetrieveStatus(oldName).ShouldEqual(FileStatus.Nonexistent);

                File.WriteAllText(oldPath, "hello test file\n", Encoding.ASCII);
                repo.Index.RetrieveStatus(oldName).ShouldEqual(FileStatus.Untracked);

                repo.Index.Stage(oldName);
                repo.Index.RetrieveStatus(oldName).ShouldEqual(FileStatus.Added);

                // Generated through
                // $ echo "hello test file" | git hash-object --stdin
                const string expectedHash = "88df547706c30fa19f02f43cb2396e8129acfd9b";
                repo.Index[oldName].Id.Sha.ShouldEqual((expectedHash));

                repo.Index.Count.ShouldEqual(1);

                Signature who = Constants.Signature;
                repo.Commit("Initial commit", who, who);

                repo.Index.RetrieveStatus(oldName).ShouldEqual(FileStatus.Unaltered);

                const string newName = "being.frakking.polite.txt";

                repo.Index.Move(oldName, newName);
                repo.Index.RetrieveStatus(oldName).ShouldEqual(FileStatus.Removed);
                repo.Index.RetrieveStatus(newName).ShouldEqual(FileStatus.Added);

                repo.Index.Count.ShouldEqual(1);
                repo.Index[newName].Id.Sha.ShouldEqual((expectedHash));

                who = who.TimeShift(TimeSpan.FromMinutes(5));
                Commit commit = repo.Commit("Fix file name", who, who);

                repo.Index.RetrieveStatus(oldName).ShouldEqual(FileStatus.Nonexistent);
                repo.Index.RetrieveStatus(newName).ShouldEqual(FileStatus.Unaltered);

                commit.Tree[newName].Target.Id.Sha.ShouldEqual(expectedHash);
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
                repo.Index.RetrieveStatus(sourcePath).ShouldEqual(sourceStatus);
                repo.Index.RetrieveStatus(destPath).ShouldEqual(destStatus);

                repo.Index.Move(sourcePath, destPath);

                repo.Index.RetrieveStatus(sourcePath).ShouldEqual(sourcePostStatus);
                repo.Index.RetrieveStatus(destPath).ShouldEqual(destPostStatus);
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
                repo.Index.RetrieveStatus(sourcePath).ShouldEqual(sourceStatus);

                foreach (var destPath in destPaths)
                {
                    string path = destPath;
                    Assert.Throws<LibGit2Exception>(() => repo.Index.Move(sourcePath, path));
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

                File.Exists(fullpath).ShouldEqual(shouldInitiallyExist);
                repo.Index.RetrieveStatus(filename).ShouldEqual(initialStatus);

                repo.Index.Remove(filename);

                repo.Index.Count.ShouldEqual(count - 1);
                Assert.False(File.Exists(fullpath));
                repo.Index.RetrieveStatus(filename).ShouldEqual(finalStatus);
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
                Assert.Throws<LibGit2Exception>(() => repo.Index.Remove(filepath));
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
                ie.ShouldNotBeNull();
                
                // Make sure that the (native) relFilePath and ie.Path are equal
                ie.Path.ShouldEqual(relFilePath);
            }
        }
    }
}
