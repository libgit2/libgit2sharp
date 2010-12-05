using NUnit.Framework;
using System.IO;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class RepositoryFixtures
    {
        const string PathToRepository = "../../Resources/testrepo.git";

        [Test]
        public void Ctor_RetrieveDetails()
        {
            using (var repo = new Repository(PathToRepository))
            {
                Assert.AreEqual(true, repo.Details.IsBare);

                var gitDir = new DirectoryInfo(PathToRepository);
                Assert.AreEqual(gitDir, new DirectoryInfo(repo.Details.RepositoryDirectory));

                var odb = new DirectoryInfo(repo.Details.DatabaseDirectory);
                Assert.AreEqual("objects", odb.Name);
            }
        }

        [Test]
        public void AnExistingObjectCanBeLookedUp()
        {
            const string objectId = "8496071c1b46c854b31185ea97743be6a8774479";
            GitObject gitObject;

            using (var repo = new Repository(PathToRepository))
            {
                gitObject = repo.Lookup(objectId);
            }

            Assert.IsNotNull(gitObject);
            Assert.AreEqual(objectId, gitObject.Id);
        }

        [Test]
        [Ignore]
        public void ANonExistingObjectCanNotBeLookedUp()
        {
            const string objectId = "a496071c1b46c854b31185ea97743be6a8774471";
            GitObject gitObject;

            using (var repo = new Repository(PathToRepository))
            {
                gitObject = repo.Lookup(objectId);
            }

            Assert.IsNull(gitObject);
        }

        [Test]
        public void HeadersOfAnExistingObjectCanBeRead()
        {
            const string objectId = "8496071c1b46c854b31185ea97743be6a8774479";
            Header header;

            using (var repo = new Repository(PathToRepository))
            {
                header = repo.ReadHeader(objectId);
            }

            Assert.IsNotNull(header);
            Assert.AreEqual(objectId, header.Id);
            Assert.AreEqual(ObjectType.Commit, header.Type);
            Assert.AreEqual(172, header.Length);
        }

        [Test]
        public void AnExistingObjectCanBeRead()
        {
            const string objectId = "8496071c1b46c854b31185ea97743be6a8774479";
            RawObject rawObject;

            using (var repo = new Repository(PathToRepository))
            {
                rawObject = repo.Read(objectId);
            }

            using (var ms = new MemoryStream(rawObject.Data))
            using (var sr = new StreamReader(ms))
            {
                string content = sr.ReadToEnd();
                StringAssert.StartsWith("tree ", content);
                StringAssert.EndsWith("testing\n", content);
            }
        }
    }
}
