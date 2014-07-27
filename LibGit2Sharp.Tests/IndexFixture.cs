using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(0, repo.Index.Count);

                const string oldName = "polite.txt";

                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus(oldName));

                Touch(repo.Info.WorkingDirectory, oldName, "hello test file\n");
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
        public void PathsOfIndexEntriesAreExpressedInNativeFormat()
        {
            // Build relative path
            string relFilePath = Path.Combine("directory", "Testfile.txt");

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.WorkingDirectory, relFilePath, "Anybody out there?");

                // Stage the file
                repo.Index.Stage(relFilePath);

                // Get the index
                Index index = repo.Index;

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

        [Fact]
        public void StagingAFileWhenTheIndexIsLockedThrowsALockedFileException()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.Path, "index.lock");

                Touch(repo.Info.WorkingDirectory, "newfile", "my my, this is gonna crash\n");
                Assert.Throws<LockedFileException>(() => repo.Index.Stage("newfile"));
            }
        }

        [Fact]
        public void CanCopeWithExternalChangesToTheIndex()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            Touch(scd.DirectoryPath, "a.txt", "a\n");
            Touch(scd.DirectoryPath, "b.txt", "b\n");

            string path = Repository.Init(scd.DirectoryPath);

            using (var repoWrite = new Repository(path))
            using (var repoRead = new Repository(path))
            {
                var writeStatus = repoWrite.Index.RetrieveStatus();
                Assert.True(writeStatus.IsDirty);
                Assert.Equal(0, repoWrite.Index.Count);

                var readStatus = repoRead.Index.RetrieveStatus();
                Assert.True(readStatus.IsDirty);
                Assert.Equal(0, repoRead.Index.Count);

                repoWrite.Index.Stage("*");
                repoWrite.Commit("message", Constants.Signature, Constants.Signature);

                writeStatus = repoWrite.Index.RetrieveStatus();
                Assert.False(writeStatus.IsDirty);
                Assert.Equal(2, repoWrite.Index.Count);

                readStatus = repoRead.Index.RetrieveStatus();
                Assert.False(readStatus.IsDirty);
                Assert.Equal(2, repoRead.Index.Count);
            }
        }
    }
}
