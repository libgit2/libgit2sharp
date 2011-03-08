using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ResolvingATagReference : ReadOnlyRepositoryFixtureBase
    {
        [TestCase("refs/tags/test", "b25fa35b38051e4ae45d4222e795f9df2e43f1d1")]
        [TestCase("refs/tags/very-simple", "7b4384978d2493e851f9cca7858815fac9b10980")]
        public void ShouldReturnATag(string reference, string expectedId)
        {
            using (var repo = new Repository(PathToRepository))
            {
                var gitObject = repo.Resolve(reference);
                Assert.IsNotNull(gitObject);
                Assert.IsAssignableFrom(typeof(Tag), gitObject);
                Assert.AreEqual(ObjectType.Tag, gitObject.Type);
                Assert.AreEqual(expectedId, gitObject.Id);
            }
        }
    }
}