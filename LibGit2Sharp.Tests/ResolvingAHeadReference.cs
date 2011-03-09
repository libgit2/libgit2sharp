using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ResolvingAHeadReference : ReadOnlyRepositoryFixtureBase
    {
        [TestCase("HEAD", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        [TestCase("refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        public void ShouldReturnACommit(string reference, string expectedId)
        {
            using (var repo = new Repository(PathToRepository))
            {
                GitObject gitObject = repo.Resolve(reference);
                Assert.IsNotNull(gitObject);
                Assert.IsAssignableFrom(typeof(Commit), gitObject);
                Assert.AreEqual(ObjectType.Commit, gitObject.Type);
                Assert.AreEqual(expectedId, gitObject.Id);
            }
        }
    }
}