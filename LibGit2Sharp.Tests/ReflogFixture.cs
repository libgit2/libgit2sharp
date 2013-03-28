using LibGit2Sharp.Tests.TestHelpers;
using System.Linq;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ReflogFixture : BaseFixture
    {
        [Fact]
        public void CanReadReflog()
        {
            const int expectedReflogEntriesCount = 3;


            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                var reflog = repo.Refs.Log(repo.Refs.Head);

                Assert.Equal(expectedReflogEntriesCount, reflog.Count());

                // Initial commit assertions
                Assert.Equal("timothy.clem@gmail.com", reflog.Last().Commiter.Email);
                Assert.True(reflog.Last().Message.StartsWith("clone: from"));
                Assert.Equal(ObjectId.Zero, reflog.Last().From);

                // second commit assertions
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", reflog.ElementAt(expectedReflogEntriesCount - 2).From.Sha);
                Assert.Equal("592d3c869dbc4127fc57c189cb94f2794fa84e7e", reflog.ElementAt(expectedReflogEntriesCount - 2).To.Sha);
            }
        }

        [Fact]
        public void CannotReadReflogOnUnknownReference()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<InvalidSpecificationException>(() => repo.Refs.Log("toto").Count());
            }
        }
    }
}
