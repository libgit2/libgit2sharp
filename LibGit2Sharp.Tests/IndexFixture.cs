using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
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

        [Test]
        public void CanCountEntriesInIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                repo.Index.Count.ShouldEqual(expectedEntries.Count());
            }
        }

        [Test]
        public void CanEnumerateIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                CollectionAssert.AreEqual(expectedEntries, repo.Index.Select(e => e.Path).ToArray());
            }
        }

        [Test]
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

        [Test]
        public void FetchingAnUnknownIndexEntryReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                IndexEntry entry = repo.Index["I-do-not-exist.txt"];
                entry.ShouldBeNull();
            }
        }

        [Test]
        public void ReadIndexWithBadParamsFails()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { IndexEntry entry = repo.Index[null]; });
                Assert.Throws<ArgumentException>(() => { IndexEntry entry = repo.Index[string.Empty]; });
            }
        }

        [TestCase("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [TestCase("deleted_unstaged_file.txt", FileStatus.Missing, true, FileStatus.Removed, false, -1)]
        [TestCase("modified_unstaged_file.txt", FileStatus.Modified, true, FileStatus.Staged, true, 0)]
        [TestCase("new_untracked_file.txt", FileStatus.Untracked, false, FileStatus.Added, true, 1)]
        [TestCase("modified_staged_file.txt", FileStatus.Staged, true, FileStatus.Staged, true, 0)]
        [TestCase("new_tracked_file.txt", FileStatus.Added, true, FileStatus.Added, true, 0)]
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

        [Test]
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

        [TestCase("1/I-do-not-exist.txt", FileStatus.Nonexistent)]
        [TestCase("deleted_staged_file.txt", FileStatus.Removed)]
        public void StagingAnUnknownFileThrows(string relativePath, FileStatus status)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                repo.Index[relativePath].ShouldBeNull();
                repo.Index.RetrieveStatus(relativePath).ShouldEqual(status);

                Assert.Throws<LibGit2Exception>(() => repo.Index.Stage(relativePath));
            }
        }

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public void StageFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage(new string[] { }));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage(new string[] { null }));
            }
        }

        [TestCase("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Unaltered, true, 0)]
        [TestCase("deleted_unstaged_file.txt", FileStatus.Missing, true, FileStatus.Missing, true, 0)]
        [TestCase("modified_unstaged_file.txt", FileStatus.Modified, true, FileStatus.Modified, true, 0)]
        [TestCase("new_untracked_file.txt", FileStatus.Untracked, false, FileStatus.Untracked, false, 0)]
        [TestCase("modified_staged_file.txt", FileStatus.Staged, true, FileStatus.Modified, true, 0)]
        [TestCase("new_tracked_file.txt", FileStatus.Added, true, FileStatus.Untracked, false, -1)]
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

        [Test]
        public void CanUnstageTheRemovalOfAFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                const string filename = "deleted_staged_file.txt";

                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                File.Exists(fullPath).ShouldBeFalse();

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Removed);

                repo.Index.Unstage(filename);
                repo.Index.Count.ShouldEqual(count + 1);

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Missing);
            }
        }

        [Test]
        public void UnstagingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Unstage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Unstage((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Unstage(new string[] { }));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Unstage(new string[] { null }));
            }
        }

        [Test]
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

        [TestCase("README", FileStatus.Unaltered, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Removed, FileStatus.Staged)]
        [TestCase("new_tracked_file.txt", FileStatus.Added, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Nonexistent, FileStatus.Staged)]
        [TestCase("modified_staged_file.txt", FileStatus.Staged, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Removed, FileStatus.Staged)]
        [TestCase("modified_unstaged_file.txt", FileStatus.Modified, "deleted_unstaged_file.txt", FileStatus.Missing, FileStatus.Removed, FileStatus.Staged)]
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

        [TestCase("README", FileStatus.Unaltered, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [TestCase("new_tracked_file.txt", FileStatus.Added, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [TestCase("modified_staged_file.txt", FileStatus.Staged, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [TestCase("modified_unstaged_file.txt", FileStatus.Modified, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        public void MovingOverAnExistingFileThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        [TestCase("new_untracked_file.txt", FileStatus.Untracked, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        public void MovingAFileWichIsNotUnderSourceControlThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        [TestCase("deleted_unstaged_file.txt", FileStatus.Missing, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        [TestCase("deleted_staged_file.txt", FileStatus.Removed, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        [TestCase("i_dont_exist.txt", FileStatus.Nonexistent, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
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

        [TestCase("1/branch_file.txt", FileStatus.Unaltered, true, FileStatus.Removed)]
        [TestCase("deleted_unstaged_file.txt", FileStatus.Missing, false, FileStatus.Removed)]
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
                File.Exists(fullpath).ShouldBeFalse();
                repo.Index.RetrieveStatus(filename).ShouldEqual(finalStatus);
            }
        }

        [TestCase("deleted_staged_file.txt")]
        [TestCase("modified_unstaged_file.txt")]
        [TestCase("shadowcopy_of_an_unseen_ghost.txt")]
        public void RemovingAInvalidFileThrows(string filepath)
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Index.Remove(filepath));
            }
        }

        [Test]
        public void CanRetrieveTheStatusOfAFile()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                FileStatus status = repo.Index.RetrieveStatus("new_tracked_file.txt");
                status.ShouldEqual(FileStatus.Added);
            }
        }

        [Test]
        public void RetrievingTheStatusOfADirectoryThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => { FileStatus status = repo.Index.RetrieveStatus("1"); });
            }
        }

        [Test]
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
                CollectionAssert.AreEqual(new[] { file, "modified_unstaged_file.txt" }, status2.Modified);
                status2.Missing.Single().ShouldEqual("deleted_unstaged_file.txt");
                status2.Added.Single().ShouldEqual("new_tracked_file.txt");
                status2.Staged.Single().ShouldEqual(file);
                status2.Removed.Single().ShouldEqual("deleted_staged_file.txt");
            }
        }

        [Test]
        public void CanRetrieveTheStatusOfANewRepository()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath)) 
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

        [Test]
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
            using (var repo = Repository.Init(scd.DirectoryPath)) 
            {
                // Add the file to the index
                repo.Index.Stage(filePath);

                // Get the repository status
                RepositoryStatus repoStatus = repo.Index.RetrieveStatus();

                repoStatus.Count().ShouldEqual(1);
                var statusEntry = repoStatus.Single();

                string expectedPath = string.Format("{0}{1}{2}", directoryName, Path.DirectorySeparatorChar, fileName);
                statusEntry.FilePath.ShouldEqual(expectedPath);

                repoStatus.Added.Single().ShouldEqual(statusEntry.FilePath);
            }
        }

        [Test]
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
