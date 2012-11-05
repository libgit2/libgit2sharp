using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CheckoutFixture : BaseFixture
    {
        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("diff-test-cases")]
        public void CanCheckoutAnExistingBranch(string branchName)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                Branch branch = repo.Branches[branchName];
                Assert.NotNull(branch);

                Branch test = repo.Checkout(branch);
                Assert.False(repo.Info.IsHeadDetached);

                Assert.False(test.IsRemote);
                Assert.True(test.IsCurrentRepositoryHead);
                Assert.Equal(repo.Head, test);

                Assert.False(master.IsCurrentRepositoryHead);
            }
        }

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("diff-test-cases")]
        public void CanCheckoutAnExistingBranchByName(string branchName)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                Branch test = repo.Checkout(branchName);
                Assert.False(repo.Info.IsHeadDetached);

                Assert.False(test.IsRemote);
                Assert.True(test.IsCurrentRepositoryHead);
                Assert.Equal(repo.Head, test);

                Assert.False(master.IsCurrentRepositoryHead);
            }
        }

        [Theory]
        [InlineData("6dcf9bf")]
        [InlineData("refs/tags/lw")]
        public void CanCheckoutAnArbitraryCommit(string commitPointer)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsCurrentRepositoryHead);

                Branch detachedHead = repo.Checkout(commitPointer);

                Assert.Equal(repo.Head, detachedHead);
                Assert.Equal(repo.Lookup(commitPointer).Sha, detachedHead.Tip.Sha);
                Assert.True(repo.Head.IsCurrentRepositoryHead);
                Assert.True(repo.Info.IsHeadDetached);

                Assert.True(detachedHead.IsCurrentRepositoryHead);
                Assert.False(detachedHead.IsRemote);
                Assert.Equal(detachedHead.Name, detachedHead.CanonicalName);

                Assert.Equal("(no branch)", detachedHead.CanonicalName);

                Assert.False(master.IsCurrentRepositoryHead);
            }
        }

        [Fact]
        public void CheckingOutInABareRepoThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<InvalidOperationException>(() => repo.Checkout(repo.Branches["refs/heads/test"]));
                Assert.Throws<InvalidOperationException>(() => repo.Checkout("refs/heads/test"));
            }
        }

        [Fact]
        public void CheckingOutANonExistingBranchThrows()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Checkout("i-do-not-exist"));
            }
        }

        [Fact]
        public void CheckingOutABranchWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Checkout(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(Branch)));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(string)));
            }
        }
    }
}
