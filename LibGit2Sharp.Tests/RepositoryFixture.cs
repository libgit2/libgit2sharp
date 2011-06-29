using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RepositoryFixture : BaseFixture
    {
        private const string commitSha = "8496071c1b46c854b31185ea97743be6a8774479";

        [Test]
        public void CanCreateBareRepo()
        {
            using (var scd = new SelfCleaningDirectory())
            {
                var dir = Repository.Init(scd.DirectoryPath, true);
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();

                using (var repo = new Repository(dir))
                {
                    repo.Info.WorkingDirectory.ShouldBeNull();
                    repo.Info.Path.ShouldEqual(scd.RootedDirectoryPath + Path.DirectorySeparatorChar);
                    repo.Info.IsBare.ShouldBeTrue();

                    AssertInitializedRepository(repo);
                }
            }
        }

        [Test]
        public void CanCreateStandardRepo()
        {
            using (var scd = new SelfCleaningDirectory())
            {
                var dir = Repository.Init(scd.DirectoryPath);
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();

                using (var repo = new Repository(dir))
                {
                    repo.Info.WorkingDirectory.ShouldNotBeNull();
                    repo.Info.Path.ShouldEqual(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar));
                    repo.Info.IsBare.ShouldBeFalse();

                    AssertIsHidden(repo.Info.Path);

                    AssertInitializedRepository(repo);
                }
            }
        }

        private static void AssertIsHidden(string repoPath)
        {
            var attribs = File.GetAttributes(repoPath);
            
            (attribs & FileAttributes.Hidden).ShouldEqual(FileAttributes.Hidden);
        }

        [Test]
        public void CreatingRepoWithBadParamsThrows()
        {
            Assert.Throws<ArgumentException>(() => Repository.Init(string.Empty));
            Assert.Throws<ArgumentNullException>(() => Repository.Init(null));
        }

        private static void AssertInitializedRepository(Repository repo)
        {
            repo.Info.Path.ShouldNotBeNull();
            repo.Info.IsEmpty.ShouldBeTrue();
            repo.Info.IsHeadDetached.ShouldBeFalse();

            var headRef = repo.Refs["HEAD"];
            headRef.ShouldNotBeNull();
            headRef.TargetIdentifier.ShouldEqual("refs/heads/master");
            headRef.ResolveToDirectReference().ShouldBeNull();

            repo.Head.ShouldNotBeNull();
            repo.Head.CanonicalName.ShouldEqual(headRef.TargetIdentifier);
            repo.Head.Tip.ShouldBeNull();

            repo.Commits.Count().ShouldEqual(0);
            repo.Commits.QueryBy(new Filter { Since = repo.Head }).Count().ShouldEqual(0);
            repo.Commits.QueryBy(new Filter { Since = "HEAD" }).Count().ShouldEqual(0);
            repo.Commits.QueryBy(new Filter { Since = "refs/heads/master" }).Count().ShouldEqual(0);

            repo.Branches.Count().ShouldEqual(0);
            repo.Refs.Count().ShouldEqual(0);
            repo.Tags.Count().ShouldEqual(0);
        }


        [Test]
        public void CanOpenRepoWithFullPath()
        {
            var path = Path.GetFullPath(Constants.BareTestRepoPath);
            using (var repo = new Repository(path))
            {
                repo.ShouldNotBeNull();
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
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                repo.Info.Path.ShouldNotBeNull();
                repo.Info.WorkingDirectory.ShouldBeNull();
                repo.Info.IsBare.ShouldBeTrue();
                repo.Info.IsEmpty.ShouldBeFalse();
                repo.Info.IsHeadDetached.ShouldBeFalse();
            }
        }

        [Test]
        public void OpeningNonExistentRepoThrows()
        {
            Assert.Throws<ApplicationException>(() => { new Repository("a_bad_path"); });
        }

        [Test]
        public void OpeningRepositoryWithBadParamsThrows()
        {
            Assert.Throws<ArgumentException>(() => new Repository(string.Empty));
            Assert.Throws<ArgumentNullException>(() => new Repository(null));
        }

        [Test]
        public void CanLookupACommitByTheNameOfABranch()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var gitObject = repo.Lookup("refs/heads/master");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(gitObject);
            }
        }

        [Test]
        public void CanLookupACommitByTheNameOfALightweightTag()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var gitObject = repo.Lookup("refs/tags/lw");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(gitObject);
            }
        }

        [Test]
        public void CanLookupATagAnnotationByTheNameOfAnAnnotatedTag()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var gitObject = repo.Lookup("refs/tags/e90810b");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<TagAnnotation>(gitObject);
            }
        }

        [Test]
        public void CanLookupObjects()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<GitObject>(commitSha).ShouldNotBeNull();
            }
        }

        [Test]
        public void CanLookupSameObjectTwiceAndTheyAreEqual()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var commit = repo.Lookup(commitSha);
                var commit2 = repo.Lookup(commitSha);
                commit.Equals(commit2).ShouldBeTrue();
                commit.GetHashCode().ShouldEqual(commit2.GetHashCode());
            }
        }

        [Test]
        public void LookupObjectByWrongShaReturnsNull()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                repo.Lookup(Constants.UnknownSha).ShouldBeNull();
                repo.Lookup<GitObject>(Constants.UnknownSha).ShouldBeNull();
            }
        }

        [Test]
        public void LookupObjectByWrongTypeReturnsNull()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<TagAnnotation>(commitSha).ShouldBeNull();
            }
        }

        [Test]
        public void LookupObjectByUnknownReferenceNameReturnsNull()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                repo.Lookup("refs/heads/chopped/off").ShouldBeNull();
                repo.Lookup<GitObject>(Constants.UnknownSha).ShouldBeNull();
            }
        }

        [Test]
        public void CanLookupWhithShortIdentifers()
        {
            const string expectedAbbrevSha = "edfecad";
            const string expectedSha = expectedAbbrevSha + "02d96c9dbf64f6e238c45ddcfa762eef0";

            using (var scd = new SelfCleaningDirectory())
            {
                var dir = Repository.Init(scd.DirectoryPath);

                using (var repo = new Repository(dir))
                {
                    string filePath = Path.Combine(repo.Info.WorkingDirectory, "new.txt");

                    File.WriteAllText(filePath, "one ");
                    repo.Index.Stage(filePath);

                    var author = Constants.Signature;
                    var commit = repo.Commit(author, author, "Initial commit");

                    commit.Sha.ShouldEqual(expectedSha);

                    var lookedUp1 = repo.Lookup(expectedSha);
                    lookedUp1.ShouldEqual(commit);

                    var lookedUp2 = repo.Lookup(expectedAbbrevSha);
                    lookedUp2.ShouldEqual(commit);
                }
            }
        }

        [Test]
        public void LookingUpWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Lookup(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.Lookup<GitObject>(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup((ObjectId)null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup<Commit>((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup<Commit>((ObjectId)null));
            }
        }

        [Test]
        public void CanCheckForObjectExistence()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                repo.HasObject("8496071c1b46c854b31185ea97743be6a8774479").ShouldBeTrue();
                repo.HasObject("1385f264afb75a56a5bec74243be9b367ba4ca08").ShouldBeTrue();
                repo.HasObject("ce08fe4884650f067bd5703b6a59a8b3b3c99a09").ShouldBeFalse();
                repo.HasObject("8496071c1c46c854b31185ea97743be6a8774479").ShouldBeFalse();
            }
        }

        [Test]
        public void CheckingForObjectExistenceWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.HasObject(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.HasObject(null));
            }
        }

        [Test]
        public void CanDiscoverABareRepoGivenTheRepoPath()
        {
            string path = Repository.Discover(Constants.BareTestRepoPath);
            path.ShouldEqual(Path.GetFullPath(Constants.BareTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverABareRepoGivenASubDirectoryOfTheRepoPath()
        {
            string path = Repository.Discover(Path.Combine(Constants.BareTestRepoPath, "objects/4a"));
            path.ShouldEqual(Path.GetFullPath(Constants.BareTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverAStandardRepoGivenTheRepoPath()
        {
            string path = Repository.Discover(Constants.StandardTestRepoPath);
            path.ShouldEqual(Path.GetFullPath(Constants.StandardTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverAStandardRepoGivenASubDirectoryOfTheRepoPath()
        {
            string path = Repository.Discover(Path.Combine(Constants.StandardTestRepoPath, "objects/4a"));
            path.ShouldEqual(Path.GetFullPath(Constants.StandardTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverAStandardRepoGivenTheWorkingDirPath()
        {
            string path = Repository.Discover(Constants.StandardTestRepoWorkingDirPath);
            path.ShouldEqual(Path.GetFullPath(Constants.StandardTestRepoPath + Path.DirectorySeparatorChar));
        }
    }
}