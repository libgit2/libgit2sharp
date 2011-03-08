using System;
using System.Linq;
using NUnit.Framework;
using System.IO;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RepositoryFixtures : ReadOnlyRepositoryFixtureBase
    {
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
            const string objectId = "7b4384978d2493e851f9cca7858815fac9b10980";
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

            AssertTag_7b43849(objectId, tag);
        }

        [Test]
        public void AnExistingCommitCanBeResolvedThroughChainedTags()
        {
            // Inspired from https://github.com/libgit2/libgit2/blob/44908fe763b1a2097b65c86130ac679c458df7d2/tests/t0801-readtag.c

            const string tag1Id = "b25fa35b38051e4ae45d4222e795f9df2e43f1d1";
            const string tag2Id = "7b4384978d2493e851f9cca7858815fac9b10980";

            using (var repo = new Repository(PathToRepository))
            {
                var tag1 = repo.Resolve<Tag>(tag1Id);
                Assert.IsNotNull(tag1);
                Assert.AreEqual("test", tag1.Name);
                Assert.AreEqual(tag1Id, tag1.Id);

                Assert.IsNotNull(tag1.Target);
                Assert.AreEqual(ObjectType.Tag, tag1.Target.Type);

                var tag2 = (Tag) tag1.Target;
                Assert.AreEqual(tag2Id, tag2.Id);

                Assert.IsNotNull(tag2.Target);
                Assert.AreEqual(ObjectType.Commit, tag2.Target.Type);

                var commit = (Commit) tag2.Target;
                Assert.IsNotNull(commit.Author);
            }
        }

        [Test]
        public void AnExistingTagCanBeResolvedBySpecifyingItsExpectedType()
        {
            const string objectId = "7b4384978d2493e851f9cca7858815fac9b10980";
            Tag tag;

            using (var repo = new Repository(PathToRepository))
            {
                tag = repo.Resolve<Tag>(objectId);
            }

            AssertTag_7b43849(objectId, tag);
        }

        [Test]
        public void AnExistingCommitCanBeResolvedBySpecifyingItsExpectedType()
        {
            const string objectId = "a4a7dce85cf63874e984719f4fdd239f5145052f";
            Commit commit;

            using (var repo = new Repository(PathToRepository))
            {
                commit = repo.Resolve<Commit>(objectId);
            }

            AssertCommit_a4a7dce(objectId, commit);
        }

        private static void AssertCommit_a4a7dce(string objectId, Commit commit)
        {
            Assert.IsNotNull(commit);
            Assert.AreEqual(objectId, commit.Id);
            Assert.AreEqual(ObjectType.Commit, commit.Type);
            Assert.AreEqual("schacon@gmail.com", commit.Author.Email);
            Assert.AreEqual("Scott Chacon", commit.Committer.Name);
            Assert.AreEqual("Merge branch 'master' into br2\n", commit.Message);
            Assert.AreEqual("Merge branch 'master' into br2", commit.MessageShort);
            Assert.AreEqual(new GitDate(1274814023, -420), commit.Committer.When.ToGitDate());
            Assert.AreEqual(2, commit.Parents.Count());

        }

        private static void AssertTag_7b43849(string objectId, Tag tag)
        {
            Assert.IsNotNull(tag);
            Assert.AreEqual(objectId, tag.Id);
            Assert.AreEqual(ObjectType.Tag, tag.Type);
            Assert.AreEqual("e90810b", tag.Name);
            Assert.AreEqual("tanoku@gmail.com", tag.Tagger.Email);
            Assert.AreEqual("This is a very simple tag.\n", tag.Message);
            Assert.IsNotNull(tag.Tagger);
            Assert.IsNotNull(tag.Tagger.When);
            Assert.AreEqual(new GitDate(1281578357, 120), tag.Tagger.When.ToGitDate());
            Assert.IsNotNull(tag.Target);
            Assert.AreEqual("e90810b8df3e80c413d903f631643c716887138d", tag.Target.Id);
            Assert.AreEqual(ObjectType.Commit, tag.Target.Type);

            var targetCommit = (Commit) tag.Target;
            Assert.AreEqual(1, targetCommit.Parents.Count());
            Assert.AreEqual("6dcf9bf7541ee10456529833502442f385010c3d", targetCommit.Parents.First().Id);
            Assert.AreEqual(ObjectType.Commit, targetCommit.Parents.First().Type);

        }

        [TestCase("7b4384978d2493e851f9cca7858815fac9b10980", typeof(Tag))]
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

        [TestCase("7b4384978d2493e851f9cca7858815fac9b10980", typeof(Tag))]
        [TestCase("c47800c7266a2be04c571c04d5a6614691ea99bd", typeof(Commit))]
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

        [TestCase("7b4384978d2493e851f9cca7858815fac9b10980", typeof(Commit))]
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
