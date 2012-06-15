using Moq;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RepositoryExtensionsFixture
    {
        [Fact]
        public void CanLookupCommitBySha()
        {
            var repo = Mock.Of<IRepository>(r => r.Lookup(It.IsAny<string>(), It.IsAny<GitObjectType>()) == FakeCommit("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

            var commit = repo.Lookup<ICommit>("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");

            Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", commit.Sha);
        }

        [Fact]
        public void CanLookupCommitById()
        {
            var repo = Mock.Of<IRepository>(r => r.Lookup(It.IsAny<ObjectId>(), It.IsAny<GitObjectType>()) == FakeCommit("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

            var commit = repo.Lookup<ICommit>(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

            Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", commit.Sha);
        }

        [Theory]
        [InlineData("unit_test")]
        [InlineData("Ångström")]
        public void CanCreateBranch(string name)
        {
            var repo = new Mock<IRepository>();
            repo.Setup(r => r.Branches.Add(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644", false));

            repo.Object.CreateBranch(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");

            repo.VerifyAll();
        }

        [Fact]
        public void CanCreateBranchFromImplicitHead()
        {
            var repo = new Mock<IRepository>();
            repo.Setup(r => r.Head.CanonicalName).Returns("refs/heads/master");
            repo.Setup(r => r.Branches.Add("newBranch", "refs/heads/master", false));

            repo.Object.CreateBranch("newBranch");

            repo.VerifyAll();
        }

        [Fact]
        public void CanCreateBranchFromCommit()
        {
            var repo = new Mock<IRepository>();
            var commit = FakeCommit(new ObjectId("4c062a6361ae6959e06292c1fa5e2822d9c96345"), null, null);
            repo.Setup(r => r.Branches.Add("some-branch", "4c062a6361ae6959e06292c1fa5e2822d9c96345", false));

            repo.Object.CreateBranch("some-branch", commit);

            repo.VerifyAll();
        }

        [Fact]
        public void CanCreateCommit()
        {
            var repo = new Mock<IRepository>();
            repo.Setup(r => r.Config.Get<string>("user.name", null)).Returns("Haacked");
            repo.Setup(r => r.Config.Get<string>("user.email", null)).Returns("none@none.com");
            repo.Setup(r => r.Commit("my commit message", It.IsAny<Signature>(), It.IsAny<Signature>(), false))
                .Returns<string, Signature, Signature, bool>((m, a, c, x) => FakeCommit(m, a));

            var commit = repo.Object.Commit("my commit message");

            Assert.Equal("Haacked", commit.Author.Name);
            Assert.Equal("none@none.com", commit.Author.Email);
        }

        private static ICommit FakeCommit(string sha)
        {
            return Mock.Of<ICommit>(c => c.Sha == sha);
        }

        private static ICommit FakeCommit(string message, Signature commiterAndAuthor)
        {
            return FakeCommit(null, message, commiterAndAuthor);
        }

        private static ICommit FakeCommit(ObjectId id, string message, Signature commiterAndAuthor)
        {
            return Mock.Of<ICommit>(c => c.Id == id && c.Message == message && c.Author == commiterAndAuthor && c.Committer == commiterAndAuthor);
        }
    }
}