using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class DiffWorkdirToIndexFixture : BaseFixture
    {
        /*
         * $ git diff
         * diff --git a/deleted_unstaged_file.txt b/deleted_unstaged_file.txt
         * deleted file mode 100644
         * index f2e4113..0000000
         * --- a/deleted_unstaged_file.txt
         * +++ /dev/null
         * @@ -1 +0,0 @@
         * -stuff
         * diff --git a/modified_unstaged_file.txt b/modified_unstaged_file.txt
         * index 9217230..da6fd65 100644
         * --- a/modified_unstaged_file.txt
         * +++ b/modified_unstaged_file.txt
         * @@ -1 +1,2 @@
         * +some more text
         *  more files! more files!
         */
        [Fact]
        public void CanCompareTheWorkDirAgainstTheIndex()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                using (var changes = repo.Diff.Compare<TreeChanges>())
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Equal("deleted_unstaged_file.txt", changes.Deleted.Single().Path);
                    Assert.Equal("modified_unstaged_file.txt", changes.Modified.Single().Path);
                }
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("really-i-cant-exist.txt", FileStatus.Nonexistent)]
        public void CanCompareTheWorkDirAgainstTheIndexWithLaxUnmatchedExplicitPathsValidation(string relativePath, FileStatus currentStatus)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                using (var changes = repo.Diff.Compare<TreeChanges>(new[] { relativePath }, false, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false }))
                {
                    Assert.Empty(changes);
                }

                using (var changes = repo.Diff.Compare<TreeChanges>(new[] { relativePath }))
                {
                    Assert.Empty(changes);
                }
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("really-i-cant-exist.txt", FileStatus.Nonexistent)]
        public void ComparingTheWorkDirAgainstTheIndexWithStrictUnmatchedExplicitPathsValidationAndANonExistentPathspecThrows(string relativePath, FileStatus currentStatus)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                Assert.Throws<UnmatchedPathException>(() => repo.Diff.Compare<TreeChanges>(new[] { relativePath }, false, new ExplicitPathsOptions()));
            }
        }

        [Theory]
        [InlineData("new_untracked_file.txt", FileStatus.NewInWorkdir)]
        [InlineData("where-am-I.txt", FileStatus.Nonexistent)]
        public void CallbackForUnmatchedExplicitPathsIsCalledWhenSet(string relativePath, FileStatus currentStatus)
        {
            var callback = new AssertUnmatchedPathspecsCallbackIsCalled();

            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Equal(currentStatus, repo.RetrieveStatus(relativePath));

                using (var changes = repo.Diff.Compare<TreeChanges>(new[] { relativePath }, false, new ExplicitPathsOptions
                {
                    ShouldFailOnUnmatchedPath = false,
                    OnUnmatchedPath = callback.OnUnmatchedPath
                }))
                {
                    Assert.True(callback.WasCalled);
                }
            }
        }

        private class AssertUnmatchedPathspecsCallbackIsCalled
        {
            public bool WasCalled;

            public void OnUnmatchedPath(string unmatchedpath)
            {
                WasCalled = true;
            }
        }

        [Fact]
        public void ComparingReliesOnProvidedConfigEntriesIfAny()
        {
            const string file = "1/branch_file.txt";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                TreeEntry entry = repo.Head[file];
                Assert.Equal(Mode.ExecutableFile, entry.Mode);

                // Recreate the file in the workdir without the executable bit
                string fullpath = Path.Combine(repo.Info.WorkingDirectory, file);
                File.Delete(fullpath);
                using (var stream = ((Blob)(entry.Target)).GetContentStream())
                {
                    Touch(repo.Info.WorkingDirectory, file, stream);
                }

                // Unset the local core.filemode, if any.
                repo.Config.Unset("core.filemode", ConfigurationLevel.Local);
            }

            using (var repo = new Repository(path))
            {
                SetFilemode(repo, true);
                using (var changes = repo.Diff.Compare<TreeChanges>(new[] { file }))
                {
                    Assert.Single(changes);

                    var change = changes.Modified.Single();
                    Assert.Equal(Mode.ExecutableFile, change.OldMode);
                    Assert.Equal(Mode.NonExecutableFile, change.Mode);
                }
            }

            using (var repo = new Repository(path))
            {
                SetFilemode(repo, false);
                using (var changes = repo.Diff.Compare<TreeChanges>(new[] { file }))
                {
                    Assert.Empty(changes);
                }
            }
        }

        void SetFilemode(Repository repo, bool value)
        {
            repo.Config.Set("core.filemode", value);
        }

        [Fact]
        public void CanCompareTheWorkDirAgainstTheIndexWithUntrackedFiles()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                using (var changes = repo.Diff.Compare<TreeChanges>(null, true))
                {
                    Assert.Equal(3, changes.Count());
                    Assert.Equal("deleted_unstaged_file.txt", changes.Deleted.Single().Path);
                    Assert.Equal("modified_unstaged_file.txt", changes.Modified.Single().Path);
                    Assert.Equal("new_untracked_file.txt", changes.Added.Single().Path);
                }
            }
        }
    }
}
