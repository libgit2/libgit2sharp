using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class BlameFixture : BaseFixture
    {
        private static void AssertCorrectHeadBlame(BlameHunkCollection blame)
        {
            Assert.Single(blame);
            Assert.Equal(0, blame[0].FinalStartLineNumber);
            Assert.Equal("schacon@gmail.com", blame[0].FinalSignature.Email);
            Assert.Equal("4a202b3", blame[0].FinalCommit.Id.ToString(7));

            Assert.Equal(0, blame.HunkForLine(0).FinalStartLineNumber);
            Assert.Equal("schacon@gmail.com", blame.HunkForLine(0).FinalSignature.Email);
            Assert.Equal("4a202b3", blame.HunkForLine(0).FinalCommit.Id.ToString(7));
        }

        [Fact]
        public void CanBlameSimply()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                AssertCorrectHeadBlame(repo.Blame("README"));
            }
        }

        [Fact]
        public void CanBlameFromADifferentCommit()
        {
            string path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                // File doesn't exist at HEAD
                Assert.Throws<NotFoundException>(() => repo.Blame("ancestor-only.txt"));

                var blame = repo.Blame("ancestor-only.txt", new BlameOptions { StartingAt = "9107b30" });
                Assert.Single(blame);
            }
        }

        [Fact]
        public void ValidatesLimits()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blame = repo.Blame("README");

                Assert.Throws<ArgumentOutOfRangeException>(() => blame[1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => blame.HunkForLine(2));
            }
        }

        [Fact]
        public void CanBlameFromVariousTypes()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                AssertCorrectHeadBlame(repo.Blame("README", new BlameOptions { StartingAt = "HEAD" }));
                AssertCorrectHeadBlame(repo.Blame("README", new BlameOptions { StartingAt = repo.Head }));
                AssertCorrectHeadBlame(repo.Blame("README", new BlameOptions { StartingAt = repo.Head.Tip }));
                AssertCorrectHeadBlame(repo.Blame("README", new BlameOptions { StartingAt = repo.Branches["master"] }));
            }
        }

        [Fact]
        public void CanStopBlame()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                // $ git blame .\new.txt
                // 9fd738e8 (Scott Chacon 2010-05-24 10:19:19 -0700 1) my new file
                // (be3563a comes after 9fd738e8)
                var blame = repo.Blame("new.txt", new BlameOptions { StoppingAt = "be3563a" });
                Assert.StartsWith("be3563a", blame[0].FinalCommit.Sha);
            }
        }
    }
}
