using System;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class ApplyingATag : ReadWriteRepositoryFixtureBase
    {
        [Test]
        public void ShouldWork() // TODO: Split into different tests (returnATag, PersistTheObject, MultipleApplies, ...)
        {
            const string targetId = "8496071c1b46c854b31185ea97743be6a8774479";
            var signature = new Signature("me", "me@me.me", DateTimeOffset.Now);

            Tag appliedTag;
            using (var repo = new Repository(PathToRepository))
            {
                appliedTag = repo.ApplyTag(targetId, "tagged", "messaged", signature);
            }

            Tag retrievedTag;
            using (var repo = new Repository(PathToRepository))
            {
                retrievedTag = repo.Resolve<Tag>(appliedTag.Id);
            }

            Assert.AreEqual(appliedTag.Id, retrievedTag.Id);
            // TODO: Finalize comparison
        }
    }
}