using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class BlameFixture : BaseFixture
    {
        [Fact]
        public void CanBlameSimply()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blame = repo.Blame("README");
            
                Assert.Equal(1, blame[0].FinalStartLineNumber);
                Assert.Equal("schacon@gmail.com", blame[0].FinalSignature.Email);
                Assert.Equal("4a202b3", blame[0].FinalCommit.Id.ToString(7));

                Assert.Equal(1, blame.HunkForLine(1).FinalStartLineNumber);
                Assert.Equal("schacon@gmail.com", blame.HunkForLine(1).FinalSignature.Email);
                Assert.Equal("4a202b3", blame.HunkForLine(1).FinalCommit.Id.ToString(7));
            }
        }

        [Fact]
        public void ValidatesLimits()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blame = repo.Blame("README");

                Assert.Throws<ArgumentOutOfRangeException>(() => blame[1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => blame.HunkForLine(2));
            }
        }
    }
}
