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
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.DirectoryPath, true))
            {
                string dir = repo.Info.Path;
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();
                CheckGitConfigFile(dir);

                repo.Info.WorkingDirectory.ShouldBeNull();
                repo.Info.Path.ShouldEqual(scd.RootedDirectoryPath + Path.DirectorySeparatorChar);
                repo.Info.IsBare.ShouldBeTrue();

                AssertInitializedRepository(repo);
            }
        }

        [Test]
        public void CanCreateStandardRepo()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                string dir = repo.Info.Path;
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();
                CheckGitConfigFile(dir);

                repo.Info.WorkingDirectory.ShouldNotBeNull();
                repo.Info.Path.ShouldEqual(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar));
                repo.Info.IsBare.ShouldBeFalse();

                AssertIsHidden(repo.Info.Path);

                AssertInitializedRepository(repo);
            }
        }

        private static void CheckGitConfigFile(string dir)
        {
            string configFilePath = Path.Combine(dir, "config");
            File.Exists(configFilePath).ShouldBeTrue();

            string contents = File.ReadAllText(configFilePath);
            contents.IndexOf("repositoryformatversion = 0", StringComparison.Ordinal).ShouldNotEqual(-1);
        }

        private static void AssertIsHidden(string repoPath)
        {
            FileAttributes attribs = File.GetAttributes(repoPath);

            (attribs & FileAttributes.Hidden).ShouldEqual(FileAttributes.Hidden);
        }

        [Test]
        public void CanReinitARepository()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
        
            using (Repository repository = Repository.Init(scd.DirectoryPath))
            using (Repository repository2 = Repository.Init(scd.DirectoryPath))
            {
                repository.Info.Path.ShouldEqual(repository2.Info.Path);
            }
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

            Reference headRef = repo.Refs["HEAD"];
            headRef.ShouldNotBeNull();
            headRef.TargetIdentifier.ShouldEqual("refs/heads/master");
            headRef.ResolveToDirectReference().ShouldBeNull();

            repo.Head.ShouldNotBeNull();
            repo.Head.IsCurrentRepositoryHead.ShouldBeTrue();
            repo.Head.CanonicalName.ShouldEqual(headRef.TargetIdentifier);
            repo.Head.Tip.ShouldBeNull();

            repo.Commits.Count().ShouldEqual(0);
            repo.Commits.QueryBy(new Filter { Since = repo.Head }).Count().ShouldEqual(0);
            repo.Commits.QueryBy(new Filter { Since = "HEAD" }).Count().ShouldEqual(0);
            repo.Commits.QueryBy(new Filter { Since = "refs/heads/master" }).Count().ShouldEqual(0);
            
            repo.Head["subdir/I-do-not-exist"].ShouldBeNull();

            repo.Branches.Count().ShouldEqual(0);
            repo.Refs.Count().ShouldEqual(0);
            repo.Tags.Count().ShouldEqual(0);
        }

        [Test]
        public void CanOpenBareRepositoryThroughAFullPathToTheGitDir()
        {
            string path = Path.GetFullPath(BareTestRepoPath);
            using (var repo = new Repository(path))
            {
                repo.ShouldNotBeNull();
                repo.Info.WorkingDirectory.ShouldBeNull();
            }
        }

        [Test]
        public void CanOpenStandardRepositoryThroughAWorkingDirPath()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                repo.ShouldNotBeNull();
                repo.Info.WorkingDirectory.ShouldNotBeNull();
            }
        }

        [Test]
        public void OpeningStandardRepositoryThroughTheGitDirGuessesTheWorkingDirPath()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                repo.ShouldNotBeNull();
                repo.Info.WorkingDirectory.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanOpenRepository()
        {
            using (var repo = new Repository(BareTestRepoPath))
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
            Assert.Throws<LibGit2Exception>(() => { new Repository("a_bad_path"); });
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
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/heads/master");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(gitObject);
            }
        }

        [Test]
        public void CanLookupACommitByTheNameOfALightweightTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/tags/lw");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(gitObject);
            }
        }

        [Test]
        public void CanLookupATagAnnotationByTheNameOfAnAnnotatedTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/tags/e90810b");
                gitObject.ShouldNotBeNull();
                Assert.IsInstanceOf<TagAnnotation>(gitObject);
            }
        }

        [Test]
        public void CanLookupObjects()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<GitObject>(commitSha).ShouldNotBeNull();
            }
        }

        [Test]
        public void CanLookupSameObjectTwiceAndTheyAreEqual()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject commit = repo.Lookup(commitSha);
                GitObject commit2 = repo.Lookup(commitSha);
                commit.Equals(commit2).ShouldBeTrue();
                commit.GetHashCode().ShouldEqual(commit2.GetHashCode());
            }
        }

        [Test]
        public void LookupObjectByWrongShaReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Lookup(Constants.UnknownSha).ShouldBeNull();
                repo.Lookup<GitObject>(Constants.UnknownSha).ShouldBeNull();
            }
        }

        [Test]
        public void LookupObjectByWrongTypeReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<TagAnnotation>(commitSha).ShouldBeNull();
            }
        }

        [Test]
        public void LookupObjectByUnknownReferenceNameReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
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

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                string filePath = Path.Combine(repo.Info.WorkingDirectory, "new.txt");

                File.WriteAllText(filePath, "one ");
                repo.Index.Stage(filePath);

                Signature author = Constants.Signature;
                Commit commit = repo.Commit("Initial commit", author, author);

                commit.Sha.ShouldEqual(expectedSha);

                GitObject lookedUp1 = repo.Lookup(expectedSha);
                lookedUp1.ShouldEqual(commit);

                GitObject lookedUp2 = repo.Lookup(expectedAbbrevSha);
                lookedUp2.ShouldEqual(commit);
            }
        }

        [Test]
        public void LookingUpWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
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
        public void CanDiscoverABareRepoGivenTheRepoPath()
        {
            string path = Repository.Discover(BareTestRepoPath);
            path.ShouldEqual(Path.GetFullPath(BareTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverABareRepoGivenASubDirectoryOfTheRepoPath()
        {
            string path = Repository.Discover(Path.Combine(BareTestRepoPath, "objects/4a"));
            path.ShouldEqual(Path.GetFullPath(BareTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverAStandardRepoGivenTheRepoPath()
        {
            string path = Repository.Discover(StandardTestRepoPath);
            path.ShouldEqual(Path.GetFullPath(StandardTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverAStandardRepoGivenASubDirectoryOfTheRepoPath()
        {
            string path = Repository.Discover(Path.Combine(StandardTestRepoPath, "objects/4a"));
            path.ShouldEqual(Path.GetFullPath(StandardTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void CanDiscoverAStandardRepoGivenTheWorkingDirPath()
        {
            string path = Repository.Discover(StandardTestRepoWorkingDirPath);
            path.ShouldEqual(Path.GetFullPath(StandardTestRepoPath + Path.DirectorySeparatorChar));
        }

        [Test]
        public void DiscoverReturnsNullWhenNoRepoCanBeFound()
        {
            string path = Path.GetTempFileName();
            string suffix = "." + Guid.NewGuid().ToString().Substring(0, 7);

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory(path + suffix);
            Directory.CreateDirectory(scd.RootedDirectoryPath);
            Repository.Discover(scd.RootedDirectoryPath).ShouldBeNull();

            File.Delete(path);
        }
    }
}
