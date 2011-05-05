﻿using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RepositoryFixture
    {
        private const string newRepoPath = "new_repo";

        private const string commitSha = "8496071c1b46c854b31185ea97743be6a8774479";
        private const string notFoundSha = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";

        [Test]
        public void CanCreateBareRepo()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            {
                var dir = Repository.Init(newRepoPath, true);
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();

                using (var repo = new Repository(dir))
                {
                    repo.Info.WorkingDirectory.ShouldBeNull();
                    repo.Info.IsBare.ShouldBeTrue();

                    AssertInitializedRepository(repo);
                }
            }
        }

        [Test]
        public void CanCreateStandardRepo()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            {
                var dir = Repository.Init(newRepoPath);
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();

                using (var repo = new Repository(dir))
                {
                    repo.Info.WorkingDirectory.ShouldNotBeNull();
                    repo.Info.IsBare.ShouldBeFalse();

                    AssertInitializedRepository(repo);
                }
            }
        }

        private static void AssertInitializedRepository(Repository repo)
        {
            repo.Info.Path.ShouldNotBeNull();
            repo.Info.IsEmpty.ShouldBeTrue();
            repo.Info.IsHeadDetached.ShouldBeFalse();
            repo.Head.TargetIdentifier.ShouldEqual("refs/heads/master");
            repo.Head.ResolveToDirectReference().ShouldBeNull();
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
        public void CanLookupACommitByTheNameOfABranch()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var gitObject = repo.Lookup("refs/heads/master");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(gitObject);
            }
        }

        [Test]
        public void CanLookupACommitByTheNameOfALightweightTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var gitObject = repo.Lookup("refs/tags/lw");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(gitObject);
            }
        }

        [Test]
        public void CanLookupATagAnnotationByTheNameOfAnAnnotatedTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var gitObject = repo.Lookup("refs/tags/e90810b");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<TagAnnotation>(gitObject);
            }
        }

        [Test]
        public void CanLookupObjects()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<GitObject>(commitSha).ShouldNotBeNull();
            }
        }

        [Test]
        public void CanLookupSameObjectTwiceAndTheyAreEqual()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commit = repo.Lookup(commitSha);
                var commit2 = repo.Lookup(commitSha);
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
        public void LookupObjectByWrongShaReturnsNull()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup(notFoundSha).ShouldBeNull();
                repo.Lookup<GitObject>(notFoundSha).ShouldBeNull();
            }
        }

        [Test]
        public void LookupObjectByWrongTypeReturnsNull()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<TagAnnotation>(commitSha).ShouldBeNull();
            }
        }

        [Test]
        public void LookupObjectByUnknownReferenceNameReturnsNull()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup("refs/heads/chopped/off").ShouldBeNull();
                repo.Lookup<GitObject>(notFoundSha).ShouldBeNull();
            }
        }

        [Test]
        public void LookupWithEmptyStringThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Lookup(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.Lookup<GitObject>(string.Empty));
            }
        }

        [Test]
        public void LookupWithNullThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Lookup((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup((ObjectId)null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup<Commit>((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup<Commit>((ObjectId)null));
            }
        }

        [Test]
        [Platform(Exclude = "Linux,Unix", Reason = "No need to test windows path separators on non-windows platforms")] 
        // See http://www.nunit.org/index.php?p=platform&r=2.6 for other platforms that can be excluded/included.
        public void CanOpenRepoWithWindowsPathSeparators()
        {
            using (new Repository(@".\Resources\testrepo.git"))
            {
            }
        }

        [Test]
        public void CanOpenRepository()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Info.Path.ShouldNotBeNull();
                repo.Info.WorkingDirectory.ShouldBeNull();
                repo.Info.IsBare.ShouldBeTrue();
                repo.Info.IsEmpty.ShouldBeFalse();
                repo.Info.IsHeadDetached.ShouldBeFalse();
            }
        }

        [Test]
        public void OpenNonExistentRepoThrows()
        {
            Assert.Throws<ApplicationException>(() => { new Repository("a_bad_path"); });
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
                Assert.Throws<ArgumentNullException>(() => repo.HasObject(null));
            }
        }

        [Test]
        public void CheckForDetachedHeadOnNewRepo()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            {
                var dir = Repository.Init(newRepoPath, true);
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();

                using (var repo = new Repository(dir))
                {
                    repo.Info.IsEmpty.ShouldBeTrue();
                    repo.Info.IsHeadDetached.ShouldBeFalse();
                }
            }
        }
    }
}