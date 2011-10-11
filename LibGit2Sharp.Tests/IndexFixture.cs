using System;
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
        private readonly string[] expectedEntries = new[]
                                                        {
                                                            "1/branch_file.txt",
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
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                repo.Index.Count.ShouldEqual(expectedEntries.Count());
            }
        }

        [Test]
        public void CanEnumerateIndex()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                CollectionAssert.AreEqual(expectedEntries, repo.Index.Select(e => e.Path).ToArray());
            }
        }

        [Test]
        public void CanFetchAnIndexEntryByItsName()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                IndexEntry entry = repo.Index["README"];
                entry.Path.ShouldEqual("README");

                IndexEntry entryWithPath = repo.Index["1/branch_file.txt"];
                entryWithPath.Path.ShouldEqual("1/branch_file.txt");
            }
        }

        [Test]
        public void FetchingAnUnknwonIndexEntryReturnsNull()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                IndexEntry entry = repo.Index["I-do-not-exist.txt"];
                entry.ShouldBeNull();
            }
        }

        [Test]
        public void CanStageTheCreationOfANewFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;
                const string filename = "unit_test.txt";
                repo.Index[filename].ShouldBeNull();
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Nonexistent);

                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "some contents");
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Untracked);

                repo.Index.Stage(filename);

                repo.Index.Count.ShouldEqual(count + 1);
                repo.Index[filename].ShouldNotBeNull();
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added);
            }
        }

        [Test]
        public void CanStageTheUpdationOfANewFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
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

        [Test]
        public void StagingANewVersionOfAFileThenUnstagingRevertsTheBlobToTheVersionOfHead()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string dir = Repository.Init(scd.DirectoryPath);

            using (var repo = new Repository(dir))
            {
                repo.Index.Count.ShouldEqual(0);

                const string fileName = "subdir/myFile.txt";

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, fileName);

                const string initialContent = "Hello?";

                Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
                File.AppendAllText(fullpath, initialContent);

                repo.Index.Stage(fileName);
                ObjectId blobId = repo.Index[fileName].Id;

                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);
                repo.Index.Count.ShouldEqual(1);

                File.AppendAllText(fullpath, "Is there there anybody out there?");
                repo.Index.Stage(fileName);

                repo.Index.Count.ShouldEqual(1);
                repo.Index[fileName].Id.ShouldNotEqual((blobId));

                repo.Index.Unstage(fileName);
                repo.Index.Count.ShouldEqual(1);
                repo.Index[fileName].Id.ShouldEqual((blobId));
            }
        }

        [Test]
        public void CanStageANewFileInAPersistentManner()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoPath);
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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
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
        public void StagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
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
        public void CanRenameAFile()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string dir = Repository.Init(scd.DirectoryPath);

            using (var repo = new Repository(dir))
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

        [Test]
        public void CanRemoveAFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                const string filename = "1/branch_file.txt";
                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                File.Exists(fullpath).ShouldBeTrue();
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Unaltered);

                repo.Index.Remove(filename);

                repo.Index.Count.ShouldEqual(count - 1);
                File.Exists(fullpath).ShouldBeFalse();
                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Removed);
            }
        }

        [Test]
        public void RemovingANonStagedFileThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Index.Remove("shadowcopy_of_an_unseen_ghost.txt"));
            }
        }

        [Test]
        public void CanUnstageANewFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                int count = repo.Index.Count;

                const string filename = "new_untracked_file.txt";
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                File.Exists(fullPath).ShouldBeTrue();

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Untracked);

                repo.Index.Stage(filename);
                repo.Index.Count.ShouldEqual(count + 1);

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Added);

                repo.Index.Unstage(filename);
                repo.Index.Count.ShouldEqual(count);

                repo.Index.RetrieveStatus(filename).ShouldEqual(FileStatus.Untracked);
            }
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanUnstageTheRemovalOfAFile()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
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
        public void CanRetrieveTheStatusOfAFile()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                FileStatus status = repo.Index.RetrieveStatus("new_tracked_file.txt");
                status.ShouldEqual(FileStatus.Added);
            }
        }

        [Test]
        public void CanRetrieveTheStatusOfTheWholeWorkingDirectory()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoWorkingDirPath);
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

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, file), "Tclem's favorite commit message: boom");

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
            string dir = Repository.Init(scd.DirectoryPath);

            using (var repo = new Repository(dir))
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
        public void UnstagingANonStagedFileThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Index.Unstage("shadowcopy_of_an_unseen_ghost.txt"));
            }
        }

        [Test]
        public void ReadIndexWithBadParamsFails()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { IndexEntry entry = repo.Index[null]; });
                Assert.Throws<ArgumentException>(() => { IndexEntry entry = repo.Index[string.Empty]; });
            }
        }

        [Test]
        public void StageFileWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage(null));
            }
        }

        [Test]
        public void UnstagingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage(null));
            }
        }
    }
}
