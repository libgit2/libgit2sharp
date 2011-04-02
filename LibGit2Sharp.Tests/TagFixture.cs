using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static readonly Signature signature = new Signature("Tim Clem", "timothy.clem@gail.com", DateTimeOffset.UtcNow);

        [Test]
        public void CanCreateTag()
        {
            using (var path = new TemporaryRepositoryPath())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("unit_test", "refs/heads/master", signature, "a new tag");
                newTag.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanCreateTagFromSha()
        {
            using (var path = new TemporaryRepositoryPath())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("unit_test", "b25fa35b38051e4ae45d4222e795f9df2e43f1d1", signature, "a new tag");
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
                    Trace.WriteLine(tag.Name);
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
                tag.Sha.ShouldEqual("b25fa35b38051e4ae45d4222e795f9df2e43f1d1");
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
                Assert.Throws<ArgumentException>(() => repo.Tags.Create("test_tag", "refs/heads/master", signature, string.Empty));
            }
        }

        [Test]
        public void CreateTagWithEmptyNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create(string.Empty, "refs/heads/master", signature, "message"));
            }
        }

        [Test]
        public void CreateTagWithEmptyTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create("test_tag", string.Empty, signature, "message"));
            }
        }

        [Test]
        public void CreateTagWithNullMessageThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", "refs/heads/master", signature, null));
            }
        }

        [Test]
        public void CreateTagWithNullNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create(null, "refs/heads/master", signature, "message"));
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
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", null, signature, "message"));
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
    }
}