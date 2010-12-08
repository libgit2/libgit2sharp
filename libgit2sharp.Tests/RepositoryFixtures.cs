using System;
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

        [Test]
        public void AnExistingObjectCanBeFound()
        {
            const string objectId = "8496071c1b46c854b31185ea97743be6a8774479";
            bool hasBeenFound;

            using (var repo = new Repository(PathToRepository))
            {
                hasBeenFound = repo.Exists(objectId);
            }

            Assert.AreEqual(true, hasBeenFound);
        }

        [Test]
        public void AnNonExistingObjectCanNotBeFound()
        {
            const string objectId = "a496071c1b46c854b31185ea97743be6a8774471";
            bool hasBeenFound;

            using (var repo = new Repository(PathToRepository))
            {
                hasBeenFound = repo.Exists(objectId);
            }

            Assert.AreEqual(false, hasBeenFound);
        }

        [Test]
        public void AnExistingTagCanBeResolvedWithoutSpecifyingItsExpectedType()
        {
            const string objectId = "0c37a5391bbff43c37f0d0371823a5509eed5b1d";
            GitObject gitObject;

            using (var repo = new Repository(PathToRepository))
            {
                gitObject = repo.Resolve(objectId);
            }

            Assert.IsNotNull(gitObject);
            Assert.AreEqual(objectId, gitObject.Id);
            Assert.AreEqual(ObjectType.Tag, gitObject.Type);
            Assert.IsAssignableFrom<Tag>(gitObject);

            var tag = gitObject as Tag;

            AssertTag0c37a53(objectId, tag);
        }

        [Test]
        public void AnExistingTagCanBeResolvedBySpecifyingItsExpectedType()
        {
            const string objectId = "0c37a5391bbff43c37f0d0371823a5509eed5b1d";
            Tag tag;

            using (var repo = new Repository(PathToRepository))
            {
                tag = repo.Resolve<Tag>(objectId);
            }

            AssertTag0c37a53(objectId, tag);
        }

        [Test]
        public void AnExistingCommitCanBeResolvedBySpecifyingItsExpectedType()
        {
            const string objectId = "36060c58702ed4c2a40832c51758d5344201d89a";
            Commit commit;

            using (var repo = new Repository(PathToRepository))
            {
                commit = repo.Resolve<Commit>(objectId);
            }

            AssertCommit36060c5(objectId, commit);
        }

        private static void AssertCommit36060c5(string objectId, Commit commit)
        {
            Assert.IsNotNull(commit);
            Assert.AreEqual(objectId, commit.Id);
            Assert.AreEqual(ObjectType.Commit, commit.Type);
            Assert.Fail("To be finalized.");
        }

        private static void AssertTag0c37a53(string objectId, Tag tag)
        {
            Assert.IsNotNull(tag);
            Assert.AreEqual(objectId, tag.Id);
            Assert.AreEqual(ObjectType.Tag, tag.Type);
            Assert.AreEqual("v1.0", tag.Name);
            Assert.AreEqual("schacon@gmail.com", tag.Tagger.Email);
            Assert.AreEqual("test tag message\n", tag.Message);
            Assert.AreEqual(1288114383, tag.Tagger.Time);
            Assert.AreEqual("5b5b025afb0b4c913b4c338a42934a3863bf3644", tag.Target.Id);
        }

        [TestCase("0c37a5391bbff43c37f0d0371823a5509eed5b1d", typeof(Tag))]
        [TestCase("8496071c1b46c854b31185ea97743be6a8774479", typeof(Commit))]
        public void ShouldResolveWhenSpecifyingAValidObjectIdAndAValidExpectedType(string objectId, Type expectedType)
        {
            object gitObject;
            using (var repo = new Repository(PathToRepository))
            {
                gitObject = repo.Resolve(objectId, expectedType);
            }

            Assert.IsNotNull(gitObject);
            Assert.IsInstanceOf<GitObject>(gitObject);
            Assert.IsAssignableFrom(expectedType, gitObject);
            Assert.AreEqual(objectId, ((GitObject)(gitObject)).Id);
        }

        [TestCase("0c37a5391bbff43c37f0d0371823a5509eed5b1d", typeof(Tag))]
        [TestCase("8496071c1b46c854b31185ea97743be6a8774479", typeof(Commit))]
        public void ShouldResolveWhenSpecifyingAValidObjectId(string objectId, Type expectedType)
        {
            object gitObject;
            using (var repo = new Repository(PathToRepository))
            {
                gitObject = repo.Resolve(objectId);
            }

            Assert.IsNotNull(gitObject);
            Assert.IsInstanceOf<GitObject>(gitObject);
            Assert.IsAssignableFrom(expectedType, gitObject);
            Assert.AreEqual(objectId, ((GitObject)(gitObject)).Id);
        }

        [TestCase("deadbeef1bbff43c37f0d0371823a5509eed5b1d", typeof(Tag))]
        public void ShouldNotResolveWhenSpecifyingAnInvalidObjectIdAndAValidExpectedType(string objectId, Type expectedType)
        {
            object gitObject;
            using (var repo = new Repository(PathToRepository))
            {
                gitObject = repo.Resolve(objectId, expectedType);
            }

            Assert.IsNull(gitObject);
        }

        [TestCase("0c37a5391bbff43c37f0d0371823a5509eed5b1d", typeof(Commit))]
        public void ShouldNotResolveWhenSpecifyingAValidObjectIdAndAnInvalidExpectedType(string objectId, Type expectedType)
        {
            object gitObject;
            using (var repo = new Repository(PathToRepository))
            {
                gitObject = repo.Resolve(objectId, expectedType);
            }

            Assert.IsNull(gitObject);
        }

    }
}
