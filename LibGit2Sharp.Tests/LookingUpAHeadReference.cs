using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class LookingUpAHeadReference : ReadOnlyRepositoryFixtureBase
    {
        [TestCase("HEAD", true, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        [TestCase("HEAD", false, "refs/heads/master")]
        [TestCase("head-tracker", true, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        [TestCase("head-tracker", false, "HEAD")]
        [TestCase("refs/heads/master", true, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        public void ShouldReturnARef(string referenceName, bool shouldPeel, string expectedTarget)
        {
            using (var repo = new Repository(PathToRepository))
            {
                Ref reference = repo.Refs.Lookup(referenceName, shouldPeel);
                Assert.IsNotNull(reference);
                Assert.AreEqual(referenceName, reference.CanonicalName);
                Assert.AreEqual(expectedTarget, reference.Target);
            }
        }
    }
}