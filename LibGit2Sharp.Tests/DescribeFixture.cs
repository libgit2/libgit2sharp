using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DescribeFixture : BaseFixture
    {
        [Fact]
        public void CanDescribeACommit()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                // No annotated tags can be used to describe "master"
                var masterTip = repo.Branches["master"].Tip;
                Assert.Throws<NotFoundException>(() => repo.Describe(masterTip));
                Assert.Equal("4c062a6", repo.Describe(masterTip,
                    new DescribeOptions { UseCommitIdAsFallback = true }));
                Assert.Equal("4c06", repo.Describe(masterTip,
                    new DescribeOptions { UseCommitIdAsFallback = true, MinimumCommitIdAbbreviatedSize = 2 }));

                // No lightweight tags can either be used to describe "master"
                Assert.Throws<NotFoundException>(() => repo.Describe(masterTip,
                    new DescribeOptions{ Strategy = DescribeStrategy.Tags }));

                repo.ApplyTag("myTag", "5b5b025afb0b4c913b4c338a42934a3863bf3644");
                Assert.Equal("myTag-5-g4c062a6", repo.Describe(masterTip,
                    new DescribeOptions { Strategy = DescribeStrategy.Tags }));
                Assert.Equal("myTag-5-g4c062a636", repo.Describe(masterTip,
                    new DescribeOptions { Strategy = DescribeStrategy.Tags, MinimumCommitIdAbbreviatedSize = 9 }));
                Assert.Equal("myTag-4-gbe3563a", repo.Describe(masterTip.Parents.Single(),
                    new DescribeOptions { Strategy = DescribeStrategy.Tags }));

                Assert.Equal("heads/master", repo.Describe(masterTip,
                    new DescribeOptions { Strategy = DescribeStrategy.All }));
                Assert.Equal("heads/packed-test-3-gbe3563a", repo.Describe(masterTip.Parents.Single(),
                    new DescribeOptions { Strategy = DescribeStrategy.All }));

                // "test" branch points to an annotated tag (also named "test")
                // Let's rename the branch to ease the understanding of what we
                // are exercising.

                repo.Branches.Rename(repo.Branches["test"], "ForLackOfABetterName");

                var anotherTip = repo.Branches["ForLackOfABetterName"].Tip;
                Assert.Equal("test", repo.Describe(anotherTip));
                Assert.Equal("test-0-g7b43849", repo.Describe(anotherTip,
                    new DescribeOptions{ AlwaysRenderLongFormat = true }));
            }
        }
    }
}
