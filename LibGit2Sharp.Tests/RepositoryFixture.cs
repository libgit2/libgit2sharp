using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RepositoryFixture
    {
        private const string newRepoPath = "new_repo";

        private const string commitSha = "8496071c1b46c854b31185ea97743be6a8774479";
        private const string notFoundSha = "ce08fe4884650f067bd5703b6a59a8b3b3c99a09";

        [Test]
        public void CallingExistsWithEmptyThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.HasObject(string.Empty));
            }
        }

        [Test]
        public void CallingExistsWithNullThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.HasObject((string) null));
            }
        }

        [Test]
        public void CanCreateRepo()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            {
                var dir = Repository.Init(newRepoPath);
                Directory.Exists(dir).ShouldBeTrue();
                using (new Repository(Path.Combine(dir, ".git")))
                {
                }
            }
        }

        [Test]
        public void CanLookupByReference()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup("refs/heads/master").ShouldNotBeNull();
            }
        }

        [Test]
        public void CanLookupObjects()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.TryLookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.TryLookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<GitObject>(commitSha).ShouldNotBeNull();
                repo.TryLookup<GitObject>(commitSha).ShouldNotBeNull();

                Assert.Throws<KeyNotFoundException>(() => repo.Lookup(notFoundSha));
                Assert.Throws<KeyNotFoundException>(() => repo.Lookup<GitObject>(notFoundSha));
                repo.TryLookup(notFoundSha).ShouldBeNull();
                repo.TryLookup<GitObject>(notFoundSha).ShouldBeNull();
            }
        }

        [Test]
        public void CanLookupSameObjectTwiceAndTheyAreEqual()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commit = repo.Lookup(commitSha);
                var commit2 = repo.TryLookup(commitSha);
                commit.Equals(commit2).ShouldBeTrue();
                commit.GetHashCode().ShouldEqual(commit2.GetHashCode());
            }
        }

        [Test]
        public void CanOpenRepoWithFullPath()
        {
            var path = Path.GetFullPath(Constants.TestRepoPath);
            using (new Repository(path))
            {
            }
        }

        [Test]
        public void CanOpenRepository()
        {
            using (new Repository(Constants.TestRepoPath))
            {
            }
        }

        [Test]
        public void CanTellIfObjectsExistInRepository()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.HasObject("8496071c1b46c854b31185ea97743be6a8774479").ShouldBeTrue();
                repo.HasObject("1385f264afb75a56a5bec74243be9b367ba4ca08").ShouldBeTrue();
                repo.HasObject("ce08fe4884650f067bd5703b6a59a8b3b3c99a09").ShouldBeFalse();
                repo.HasObject("8496071c1c46c854b31185ea97743be6a8774479").ShouldBeFalse();
            }
        }

        [Test]
        public void CreateRepoWithEmptyStringThrows()
        {
            Assert.Throws<ArgumentException>(() => Repository.Init(string.Empty));
        }

        [Test]
        public void CreateRepoWithNullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => Repository.Init(null));
        }

        [Test]
        [Ignore("TODO: fix libgit2 error handling for this to work.")]
        public void LookupObjectByWrongTypeThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup<Tag>(commitSha);
            }
        }

        [Test]
        public void LookupWithEmptyStringThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Lookup(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.Lookup<GitObject>(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.TryLookup(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.TryLookup<GitObject>(string.Empty));
            }
        }

        [Test]
        public void LookupWithNullThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Lookup((string) null));
                Assert.Throws<ArgumentNullException>(() => repo.TryLookup((string) null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup<Commit>((string) null));
                Assert.Throws<ArgumentNullException>(() => repo.TryLookup<Commit>(null));
            }
        }

        [Test]
        public void OpenNonExistentRepoThrows()
        {
            Assert.Throws<ArgumentException>(() => { new Repository("a_bad_path"); });
        }

        [Test]
        public void OpeningRepositoryWithEmptyPathThrows()
        {
            Assert.Throws<ArgumentException>(() => new Repository(string.Empty));
        }

        [Test]
        public void OpeningRepositoryWithNullPathThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new Repository(null));
        }
    }
}