using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RepositoryFixture : BaseFixture
    {
        private const string commitSha = "8496071c1b46c854b31185ea97743be6a8774479";

        [Fact]
        public void CanCreateBareRepo()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.DirectoryPath, true))
            {
                string dir = repo.Info.Path;
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();
                CheckGitConfigFile(dir);

                Assert.Null(repo.Info.WorkingDirectory);
                Assert.Equal(scd.RootedDirectoryPath + Path.DirectorySeparatorChar, repo.Info.Path);
                repo.Info.IsBare.ShouldBeTrue();

                AssertInitializedRepository(repo);
            }
        }

        [Fact]
        public void AccessingTheIndexInABareRepoThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Index);
            }
        }

        [Fact]
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
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);

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

            Assert.Equal(FileAttributes.Hidden, (attribs & FileAttributes.Hidden));
        }

        [Fact]
        public void CanReinitARepository()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
        
            using (Repository repository = Repository.Init(scd.DirectoryPath))
            using (Repository repository2 = Repository.Init(scd.DirectoryPath))
            {
                Assert.Equal(repository2.Info.Path, repository.Info.Path);
            }
        }

        [Fact]
        public void CreatingRepoWithBadParamsThrows()
        {
            Assert.Throws<ArgumentException>(() => Repository.Init(string.Empty));
            Assert.Throws<ArgumentNullException>(() => Repository.Init(null));
        }

        private static void AssertInitializedRepository(Repository repo)
        {
            repo.Info.Path.ShouldNotBeNull();
            repo.Info.IsEmpty.ShouldBeTrue();
            Assert.False(repo.Info.IsHeadDetached);

            Reference headRef = repo.Refs["HEAD"];
            headRef.ShouldNotBeNull();
            Assert.Equal("refs/heads/master", headRef.TargetIdentifier);
            Assert.Null(headRef.ResolveToDirectReference());

            repo.Head.ShouldNotBeNull();
            repo.Head.IsCurrentRepositoryHead.ShouldBeTrue();
            Assert.Equal(headRef.TargetIdentifier, repo.Head.CanonicalName);
            Assert.Null(repo.Head.Tip);

            Assert.Equal(0, repo.Commits.Count());
            Assert.Equal(0, repo.Commits.QueryBy(new Filter { Since = repo.Head }).Count());
            Assert.Equal(0, repo.Commits.QueryBy(new Filter { Since = "HEAD" }).Count());
            Assert.Equal(0, repo.Commits.QueryBy(new Filter { Since = "refs/heads/master" }).Count());

            Assert.Null(repo.Head["subdir/I-do-not-exist"]);

            Assert.Equal(0, repo.Branches.Count());
            Assert.Equal(0, repo.Refs.Count());
            Assert.Equal(0, repo.Tags.Count());
        }

        [Fact]
        public void CanOpenBareRepositoryThroughAFullPathToTheGitDir()
        {
            string path = Path.GetFullPath(BareTestRepoPath);
            using (var repo = new Repository(path))
            {
                repo.ShouldNotBeNull();
                Assert.Null(repo.Info.WorkingDirectory);
            }
        }

        [Fact]
        public void CanOpenStandardRepositoryThroughAWorkingDirPath()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                repo.ShouldNotBeNull();
                repo.Info.WorkingDirectory.ShouldNotBeNull();
            }
        }

        [Fact]
        public void OpeningStandardRepositoryThroughTheGitDirGuessesTheWorkingDirPath()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                repo.ShouldNotBeNull();
                repo.Info.WorkingDirectory.ShouldNotBeNull();
            }
        }

        [Fact]
        public void CanOpenRepository()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Info.Path.ShouldNotBeNull();
                Assert.Null(repo.Info.WorkingDirectory);
                repo.Info.IsBare.ShouldBeTrue();
                Assert.False(repo.Info.IsEmpty);
                Assert.False(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void OpeningNonExistentRepoThrows()
        {
            Assert.Throws<LibGit2Exception>(() => { new Repository("a_bad_path"); });
        }

        [Fact]
        public void OpeningRepositoryWithBadParamsThrows()
        {
            Assert.Throws<ArgumentException>(() => new Repository(string.Empty));
            Assert.Throws<ArgumentNullException>(() => new Repository(null));
        }

        [Fact]
        public void CanLookupACommitByTheNameOfABranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/heads/master");
                gitObject.ShouldNotBeNull();
                Assert.IsType<Commit>(gitObject);
            }
        }

        [Fact]
        public void CanLookupACommitByTheNameOfALightweightTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/tags/lw");
                gitObject.ShouldNotBeNull();
                Assert.IsType<Commit>(gitObject);
            }
        }

        [Fact]
        public void CanLookupATagAnnotationByTheNameOfAnAnnotatedTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/tags/e90810b");
                gitObject.ShouldNotBeNull();
                Assert.IsType<TagAnnotation>(gitObject);
            }
        }

        [Fact]
        public void CanLookupObjects()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<GitObject>(commitSha).ShouldNotBeNull();
            }
        }

        [Fact]
        public void CanLookupSameObjectTwiceAndTheyAreEqual()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject commit = repo.Lookup(commitSha);
                GitObject commit2 = repo.Lookup(commitSha);
                commit.Equals(commit2).ShouldBeTrue();
                Assert.Equal(commit2.GetHashCode(), commit.GetHashCode());
            }
        }

        [Fact]
        public void LookupObjectByWrongShaReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Null(repo.Lookup(Constants.UnknownSha));
                Assert.Null(repo.Lookup<GitObject>(Constants.UnknownSha));
            }
        }

        [Fact]
        public void LookupObjectByWrongTypeReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                Assert.Null(repo.Lookup<TagAnnotation>(commitSha));
            }
        }

        [Fact]
        public void LookupObjectByUnknownReferenceNameReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Null(repo.Lookup("refs/heads/chopped/off"));
                Assert.Null(repo.Lookup<GitObject>(Constants.UnknownSha));
            }
        }

        [Fact]
        public void CanLookupWhithShortIdentifers()
        {
            const string expectedAbbrevSha = "fe8410b";
            const string expectedSha = expectedAbbrevSha + "6bfdf69ccfd4f397110d61f8070e46e40";

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                string filePath = Path.Combine(repo.Info.WorkingDirectory, "new.txt");

                File.WriteAllText(filePath, "one ");
                repo.Index.Stage(filePath);

                Signature author = Constants.Signature;
                Commit commit = repo.Commit("Initial commit", author, author);

                Assert.Equal(expectedSha, commit.Sha);

                GitObject lookedUp1 = repo.Lookup(expectedSha);
                Assert.Equal(commit, lookedUp1);

                GitObject lookedUp2 = repo.Lookup(expectedAbbrevSha);
                Assert.Equal(commit, lookedUp2);
            }
        }

        [Fact]
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

        [Fact]
        public void CanDiscoverABareRepoGivenTheRepoPath()
        {
            string path = Repository.Discover(BareTestRepoPath);
            Assert.Equal(Path.GetFullPath(BareTestRepoPath + Path.DirectorySeparatorChar), path);
        }

        [Fact]
        public void CanDiscoverABareRepoGivenASubDirectoryOfTheRepoPath()
        {
            string path = Repository.Discover(Path.Combine(BareTestRepoPath, "objects/4a"));
            Assert.Equal(Path.GetFullPath(BareTestRepoPath + Path.DirectorySeparatorChar), path);
        }

        [Fact]
        public void CanDiscoverAStandardRepoGivenTheRepoPath()
        {
            string path = Repository.Discover(StandardTestRepoPath);
            Assert.Equal(Path.GetFullPath(StandardTestRepoPath + Path.DirectorySeparatorChar), path);
        }

        [Fact]
        public void CanDiscoverAStandardRepoGivenASubDirectoryOfTheRepoPath()
        {
            string path = Repository.Discover(Path.Combine(StandardTestRepoPath, "objects/4a"));
            Assert.Equal(Path.GetFullPath(StandardTestRepoPath + Path.DirectorySeparatorChar), path);
        }

        [Fact]
        public void CanDiscoverAStandardRepoGivenTheWorkingDirPath()
        {
            string path = Repository.Discover(StandardTestRepoWorkingDirPath);
            Assert.Equal(Path.GetFullPath(StandardTestRepoPath + Path.DirectorySeparatorChar), path);
        }

        [Fact]
        public void DiscoverReturnsNullWhenNoRepoCanBeFound()
        {
            string path = Path.GetTempFileName();
            string suffix = "." + Guid.NewGuid().ToString().Substring(0, 7);

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory(path + suffix);
            Directory.CreateDirectory(scd.RootedDirectoryPath);
            Assert.Null(Repository.Discover(scd.RootedDirectoryPath));

            File.Delete(path);
        }
    }
}
