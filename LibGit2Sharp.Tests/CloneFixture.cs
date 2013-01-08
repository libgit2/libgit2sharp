using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CloneFixture : BaseFixture
    {
        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        //[InlineData("git@github.com:libgit2/TestGitRepository")]
        public void CanClone(string url)
        {
            var scd = BuildSelfCleaningDirectory();
            using (Repository repo = Repository.Clone(url, scd.RootedDirectoryPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);

                Assert.True(File.Exists(Path.Combine(scd.RootedDirectoryPath, "master.txt")));
                Assert.Equal(repo.Head.Name, "master");
                Assert.Equal(repo.Head.Tip.Id.ToString(), "49322bb17d3acc9146f98c97d078513228bbf3c0");
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        //[InlineData("git@github.com:libgit2/TestGitRepository")]
        public void CanCloneBarely(string url)
        {
            var scd = BuildSelfCleaningDirectory();
            using (Repository repo = Repository.Clone(url, scd.RootedDirectoryPath, bare: true))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.Null(repo.Info.WorkingDirectory);
                Assert.Equal(scd.RootedDirectoryPath + Path.DirectorySeparatorChar, repo.Info.Path);
                Assert.True(repo.Info.IsBare);
            }
        }

        [Theory]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        public void WontCheckoutIfAskedNotTo(string url)
        {
            var scd = BuildSelfCleaningDirectory();
            using (Repository repo = Repository.Clone(url, scd.RootedDirectoryPath, checkout: false))
            {
                Assert.False(File.Exists(Path.Combine(scd.RootedDirectoryPath, "master.txt")));
            }
        }

        [Theory]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        public void CallsProgressCallbacks(string url)
        {
            bool transferWasCalled = false;
            bool checkoutWasCalled = false;

            var scd = BuildSelfCleaningDirectory();
            using (Repository repo = Repository.Clone(url, scd.RootedDirectoryPath,
                onTransferProgress: (_) => transferWasCalled = true,
                onCheckoutProgress: (a, b, c) => checkoutWasCalled = true))
            {
                Assert.True(transferWasCalled);
                Assert.True(checkoutWasCalled);
            }
        }
    }
}
