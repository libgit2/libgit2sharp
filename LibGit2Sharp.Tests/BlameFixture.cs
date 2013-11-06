using System.IO;
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
            }
        }
    }
}