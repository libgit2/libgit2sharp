using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class TagFixture
    {
        private readonly List<string> expectedTags = new List<string> { "test", "e90810b" };

        private static readonly Signature signatureTim = new Signature("Tim Clem", "timothy.clem@gmail.com", DateTimeOffset.UtcNow);
        private static readonly Signature signatureNtk = new Signature("nulltoken", "emeric.fermas@gmail.com", Epoch.ToDateTimeOffset(1300557894, 60));
        private const string targetSha = "b25fa35b38051e4ae45d4222e795f9df2e43f1d1";

        [Test]
        public void CanCreateTag()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("unit_test", "refs/heads/master", signatureTim, "a new tag");
                newTag.ShouldNotBeNull();
            }
        }

        [Test]
        public void CreateTagIsDeterministic()
        {
            const string tagTargetSha = "e90810b8df3e80c413d903f631643c716887138d";
            const string tagName = "nullTAGen";
            const string tagMessage = "I've been tagged!";

            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create(tagName, tagTargetSha, signatureNtk, tagMessage);
                newTag.Sha.ShouldEqual("24f6de34a108d931c6056fc4687637fe36c6bd6b");
            }
        }

        [Test]
        public void CanCreateTagFromSha()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("unit_test", targetSha, signatureTim, "a new tag");
                newTag.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanListTags()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var tag in repo.Tags)
                {
                    expectedTags.Contains(tag.Name).ShouldBeTrue();
                }
                repo.Tags.Count().ShouldEqual(2);
            }
        }

        [Test]
        public void CanLookupTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tag = repo.Tags["test"];
                tag.ShouldNotBeNull();
                tag.Name.ShouldEqual("test");
                tag.Sha.ShouldEqual(targetSha);
                tag.Tagger.Email.ShouldEqual("tanoku@gmail.com");
                tag.Tagger.Name.ShouldEqual("Vicent Marti");
                tag.Tagger.When.ToSecondsSinceEpoch().ShouldEqual(1281578440);
                tag.Message.ShouldEqual("This is a test tag\n");
            }
        }

        [Test]
        public void CreateTagWithEmptyMessageThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create("test_tag", "refs/heads/master", signatureTim, string.Empty));
            }
        }

        [Test]
        public void CreateTagWithEmptyNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create(string.Empty, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithEmptyTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create("test_tag", string.Empty, signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithNullMessageThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", "refs/heads/master", signatureTim, null));
            }
        }

        [Test]
        public void CreateTagWithNullNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create(null, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithNullSignatureThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", "refs/heads/master", null, "message"));
            }
        }

        [Test]
        public void CreateTagWithNullTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", null, signatureTim, "message"));
            }
        }

        [Test]
        public void LookupEmptyTagNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => { var t = repo.Tags[string.Empty]; });
            }
        }

        [Test]
        public void LookupNullTagNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { var t = repo.Tags[null]; });
            }
        }

        [Test]
        public void CreateTagWithNotExistingTargetThrows()
        {
            const string invalidTargetId = "deadbeef1b46c854b31185ea97743be6a8774479";

            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Tags.Create("test_tag", invalidTargetId, signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithADuplicateNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Tags.Create("test", targetSha, signatureTim, "message"));
            }
        }
    }
}