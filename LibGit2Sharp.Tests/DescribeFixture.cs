using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using System;

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

                repo.ApplyTag("myOldestTag", "8496071c1b46c854b31185ea97743be6a8774479");
                Assert.Equal("myOldestTag-6-g4c062a6", repo.Describe(masterTip,
                    new DescribeOptions { Strategy = DescribeStrategy.Tags, Match = "myOld*"}));

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

        [Fact]
        public void CanFollowFirstParent()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var branch = repo.CreateBranch("branch");

                // Make an earlier tag on master
                repo.Commit("A", Constants.Signature, Constants.Signature, new CommitOptions { AllowEmptyCommit = true });
                repo.ApplyTag("firstParentTag");

                // Make a later tag on branch
                Commands.Checkout(repo, branch);
                repo.Commit("B", Constants.Signature, Constants.Signature, new CommitOptions { AllowEmptyCommit = true });
                repo.ApplyTag("mostRecentTag");

                Commands.Checkout(repo, "master");
                repo.Commit("C", Constants.Signature, Constants.Signature, new CommitOptions { AllowEmptyCommit = true });
                repo.Merge(branch, Constants.Signature, new MergeOptions() { FastForwardStrategy = FastForwardStrategy.NoFastForward });

                // With OnlyFollowFirstParent = false, the most recent tag reachable should be returned
                Assert.Equal("mostRecentTag-3-gf17be71", repo.Describe(repo.Head.Tip, new DescribeOptions { OnlyFollowFirstParent = false, Strategy = DescribeStrategy.Tags }));

                // With OnlyFollowFirstParent = true, the most recent tag on the current branch should be returned
                Assert.Equal("firstParentTag-2-gf17be71", repo.Describe(repo.Head.Tip, new DescribeOptions { OnlyFollowFirstParent = true, Strategy = DescribeStrategy.Tags }));

            }
        }
    }
}
