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
        private static readonly string subBranchFile = string.Join("/", "1", "branch_file.txt");
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedEntries.Count(), repo.Index.Count);
            }
        }

        [Fact]
        public void CanEnumerateIndex()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedEntries,
                    repo.Index.Select(e => e.Path).OrderBy(p => p, StringComparer.Ordinal).ToArray());
            }
        }

        [Fact]
        public void CanFetchAnIndexEntryByItsName()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                IndexEntry entry = repo.Index["I-do-not-exist.txt"];
                Assert.Null(entry);
            }
        }

        [Fact]
        public void ReadIndexWithBadParamsFails()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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

                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus(oldName));

                Touch(repo.Info.WorkingDirectory, oldName, "hello test file\n");
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(oldName));

                Commands.Stage(repo, oldName);
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(oldName));

                // Generated through
                // $ echo "hello test file" | git hash-object --stdin
                const string expectedHash = "88df547706c30fa19f02f43cb2396e8129acfd9b";
                Assert.Equal(expectedHash, repo.Index[oldName].Id.Sha);

                Assert.Equal(1, repo.Index.Count);

                Signature who = Constants.Signature;
                repo.Commit("Initial commit", who, who);

                Assert.Equal(FileStatus.Unaltered, repo.RetrieveStatus(oldName));

                const string newName = "being.frakking.polite.txt";

                Commands.Move(repo, oldName, newName);
                Assert.Equal(FileStatus.DeletedFromIndex, repo.RetrieveStatus(oldName));
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(newName));

                Assert.Equal(1, repo.Index.Count);
                Assert.Equal(expectedHash, repo.Index[newName].Id.Sha);

                who = who.TimeShift(TimeSpan.FromMinutes(5));
                Commit commit = repo.Commit("Fix file name", who, who);

                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus(oldName));
                Assert.Equal(FileStatus.Unaltered, repo.RetrieveStatus(newName));

                Assert.Equal(expectedHash, commit.Tree[newName].Target.Id.Sha);
            }
        }

        [Theory]
        [InlineData("README", FileStatus.Unaltered, "deleted_unstaged_file.txt", FileStatus.DeletedFromWorkdir, FileStatus.DeletedFromIndex, FileStatus.ModifiedInIndex)]
        [InlineData("new_tracked_file.txt", FileStatus.NewInIndex, "deleted_unstaged_file.txt", FileStatus.DeletedFromWorkdir, FileStatus.Nonexistent, FileStatus.ModifiedInIndex)]
        [InlineData("modified_staged_file.txt", FileStatus.ModifiedInIndex, "deleted_unstaged_file.txt", FileStatus.DeletedFromWorkdir, FileStatus.DeletedFromIndex, FileStatus.ModifiedInIndex)]
        [InlineData("modified_unstaged_file.txt", FileStatus.ModifiedInWorkdir, "deleted_unstaged_file.txt", FileStatus.DeletedFromWorkdir, FileStatus.DeletedFromIndex, FileStatus.ModifiedInIndex)]
        public void CanMoveAnExistingFileOverANonExistingFile(string sourcePath, FileStatus sourceStatus, string destPath, FileStatus destStatus, FileStatus sourcePostStatus, FileStatus destPostStatus)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(sourceStatus, repo.RetrieveStatus(sourcePath));
                Assert.Equal(destStatus, repo.RetrieveStatus(destPath));

                Commands.Move(repo, sourcePath, destPath);

                Assert.Equal(sourcePostStatus, repo.RetrieveStatus(sourcePath));
                Assert.Equal(destPostStatus, repo.RetrieveStatus(destPath));
            }
        }

        [Theory]
        [InlineData("README", FileStatus.Unaltered, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [InlineData("new_tracked_file.txt", FileStatus.NewInIndex, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [InlineData("modified_staged_file.txt", FileStatus.ModifiedInIndex, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        [InlineData("modified_unstaged_file.txt", FileStatus.ModifiedInWorkdir, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt" })]
        public void MovingOverAnExistingFileThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        public void MovingAFileWichIsNotUnderSourceControlThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        [Theory]
        [InlineData("deleted_unstaged_file.txt", FileStatus.DeletedFromWorkdir, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        [InlineData("deleted_staged_file.txt", FileStatus.DeletedFromIndex, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        [InlineData("i_dont_exist.txt", FileStatus.Nonexistent, new[] { "README", "new_tracked_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new_untracked_file.txt", "deleted_unstaged_file.txt", "deleted_staged_file.txt", "i_dont_exist.txt" })]
        public void MovingAFileNotInTheWorkingDirectoryThrows(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            InvalidMoveUseCases(sourcePath, sourceStatus, destPaths);
        }

        private void InvalidMoveUseCases(string sourcePath, FileStatus sourceStatus, IEnumerable<string> destPaths)
        {
            var repoPath = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(sourceStatus, repo.RetrieveStatus(sourcePath));

                foreach (var destPath in destPaths)
                {
                    string path = destPath;
                    Assert.Throws<LibGit2SharpException>(() => Commands.Move(repo, sourcePath, path));
                }
            }
        }

        [Fact]
        public void PathsOfIndexEntriesAreExpressedInNativeFormat()
        {
            // Build relative path
            string relFilePath = Path.Combine("directory", "Testfile.txt").Replace('\\', '/');

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.WorkingDirectory, relFilePath, "Anybody out there?");

                // Stage the file
                Commands.Stage(repo, relFilePath);

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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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
                Assert.Throws<LockedFileException>(() => Commands.Stage(repo, "newfile"));
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
                var writeStatus = repoWrite.RetrieveStatus();
                Assert.True(writeStatus.IsDirty);
                Assert.Equal(0, repoWrite.Index.Count);

                var readStatus = repoRead.RetrieveStatus();
                Assert.True(readStatus.IsDirty);
                Assert.Equal(0, repoRead.Index.Count);

                Commands.Stage(repoWrite, "*");
                repoWrite.Commit("message", Constants.Signature, Constants.Signature);

                writeStatus = repoWrite.RetrieveStatus();
                Assert.False(writeStatus.IsDirty);
                Assert.Equal(2, repoWrite.Index.Count);

                readStatus = repoRead.RetrieveStatus();
                Assert.False(readStatus.IsDirty);
                Assert.Equal(2, repoRead.Index.Count);
            }
        }

        [Fact]
        public void CanResetFullyMergedIndexFromTree()
        {
            string path = SandboxStandardTestRepo();

            const string testFile = "new_tracked_file.txt";

            // It is sufficient to check just one of the stage area changes, such as the added file,
            // to verify that the index has indeed been read from the tree.
            using (var repo = new Repository(path))
            {
                const string headIndexTreeSha = "e5d221fc5da11a3169bf503d76497c81be3207b6";

                Assert.True(repo.Index.IsFullyMerged);
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(testFile));

                var headIndexTree = repo.Lookup<Tree>(headIndexTreeSha);
                Assert.NotNull(headIndexTree);
                var index = repo.Index;
                index.Replace(headIndexTree);
                index.Write();

                Assert.True(index.IsFullyMerged);
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(testFile));
            }

            // Check that the index was persisted to disk.
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(testFile));
            }
        }

        [Fact]
        public void CanResetIndexWithUnmergedEntriesFromTree()
        {
            string path = SandboxMergedTestRepo();

            const string testFile = "one.txt";

            // It is sufficient to check just one of the stage area changes, such as the modified file,
            // to verify that the index has indeed been read from the tree.
            using (var repo = new Repository(path))
            {
                const string headIndexTreeSha = "1cb365141a52dfbb24933515820eb3045fbca12b";

                Assert.False(repo.Index.IsFullyMerged);
                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus(testFile));

                var headIndexTree = repo.Lookup<Tree>(headIndexTreeSha);
                Assert.NotNull(headIndexTree);
                var index = repo.Index;
                index.Replace(headIndexTree);
                index.Write();

                Assert.True(index.IsFullyMerged);
                Assert.Equal(FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(testFile));
            }

            // Check that the index was persisted to disk.
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.ModifiedInWorkdir, repo.RetrieveStatus(testFile));
            }
        }

        [Fact]
        public void CanClearTheIndex()
        {
            string path = SandboxStandardTestRepo();
            const string testFile = "1.txt";

            // It is sufficient to check just one of the stage area changes, such as the modified file,
            // to verify that the index has indeed been read from the tree.
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.Unaltered, repo.RetrieveStatus(testFile));
                var index = repo.Index;
                Assert.NotEqual(0, index.Count);
                index.Clear();
                Assert.Equal(0, index.Count);
                index.Write();

                Assert.Equal(FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, repo.RetrieveStatus(testFile));
            }

            // Check that the index was persisted to disk.
            using (var repo = new Repository(path))
            {
                Assert.Equal(FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, repo.RetrieveStatus(testFile));
            }
        }

        [Theory]
        [InlineData("new_tracked_file.txt", FileStatus.NewInIndex, FileStatus.NewInWorkdir)]
        [InlineData("modified_staged_file.txt", FileStatus.ModifiedInIndex, FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir)]
        [InlineData("i_dont_exist.txt", FileStatus.Nonexistent, FileStatus.Nonexistent)]
        public void CanRemoveAnEntryFromTheIndex(string pathInTheIndex, FileStatus expectedBeforeStatus, FileStatus expectedAfterStatus)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var before = repo.RetrieveStatus(pathInTheIndex);
                Assert.Equal(expectedBeforeStatus, before);

                repo.Index.Remove(pathInTheIndex);

                var after = repo.RetrieveStatus(pathInTheIndex);
                Assert.Equal(expectedAfterStatus, after);
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir, FileStatus.NewInIndex)]
        [InlineData("modified_unstaged_file.txt", FileStatus.ModifiedInWorkdir, FileStatus.ModifiedInIndex)]
        public void CanAddAnEntryToTheIndexFromAFileInTheWorkdir(string pathInTheWorkdir, FileStatus expectedBeforeStatus, FileStatus expectedAfterStatus)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var before = repo.RetrieveStatus(pathInTheWorkdir);
                Assert.Equal(expectedBeforeStatus, before);

                repo.Index.Add(pathInTheWorkdir);

                var after = repo.RetrieveStatus(pathInTheWorkdir);
                Assert.Equal(expectedAfterStatus, after);
            }
        }

        [Fact]
        public void CanAddAnEntryToTheIndexFromABlob()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                const string targetIndexEntryPath = "1.txt";
                var before = repo.RetrieveStatus(targetIndexEntryPath);
                Assert.Equal(FileStatus.Unaltered, before);

                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                repo.Index.Add(blob, targetIndexEntryPath, Mode.NonExecutableFile);

                var after = repo.RetrieveStatus(targetIndexEntryPath);
                Assert.Equal(FileStatus.ModifiedInIndex | FileStatus.ModifiedInWorkdir, after);
            }
        }

        [Fact]
        public void AddingAnEntryToTheIndexFromAUnknwonFileInTheWorkdirThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                const string filePath = "i_dont_exist.txt";
                var before = repo.RetrieveStatus(filePath);
                Assert.Equal(FileStatus.Nonexistent, before);

                Assert.Throws<NotFoundException>(() => repo.Index.Add(filePath));
            }
        }

        [Fact]
        public void CanMimicGitAddAll()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var before = repo.RetrieveStatus();
                Assert.True(before.Any(se => se.State == FileStatus.NewInWorkdir));
                Assert.True(before.Any(se => se.State == FileStatus.ModifiedInWorkdir));
                Assert.True(before.Any(se => se.State == FileStatus.DeletedFromWorkdir));

                AddSomeCornerCases(repo);

                Commands.Stage(repo, "*");

                var after = repo.RetrieveStatus();
                Assert.False(after.Any(se => se.State == FileStatus.NewInWorkdir));
                Assert.False(after.Any(se => se.State == FileStatus.ModifiedInWorkdir));
                Assert.False(after.Any(se => se.State == FileStatus.DeletedFromWorkdir));
            }
        }

        [Fact]
        public void RetrievingAssumedUnchangedMarkedIndexEntries()
        {
            var path = SandboxAssumeUnchangedTestRepo();
            using (var repo = new Repository(path))
            {
                var regularFile = repo.Index["hello.txt"];
                Assert.False(regularFile.AssumeUnchanged);

                var assumeUnchangedFile = repo.Index["world.txt"];
                Assert.True(assumeUnchangedFile.AssumeUnchanged);
            }
        }

        private static void AddSomeCornerCases(Repository repo)
        {
            // Turn 1.txt into a directory in the Index
            repo.Index.Remove("1.txt");
            var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
            repo.Index.Add(blob, "1.txt/Sneaky", Mode.NonExecutableFile);

            // Turn README into a symlink
            Blob linkContent = OdbHelper.CreateBlob(repo, "1.txt/sneaky");
            repo.Index.Add(linkContent, "README", Mode.SymbolicLink);
        }
    }
}
