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
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));
                CheckGitConfigFile(dir);

                Assert.Null(repo.Info.WorkingDirectory);
                Assert.Equal(scd.RootedDirectoryPath + Path.DirectorySeparatorChar, repo.Info.Path);
                Assert.True(repo.Info.IsBare);

                AssertInitializedRepository(repo);
            }
        }

        [Fact]
        public void AccessingTheIndexInABareRepoThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Index);
            }
        }

        [Fact]
        public void CanCreateStandardRepo()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));
                CheckGitConfigFile(dir);

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);

                AssertIsHidden(repo.Info.Path);

                AssertInitializedRepository(repo);
            }
        }

        private static void CheckGitConfigFile(string dir)
        {
            string configFilePath = Path.Combine(dir, "config");
            Assert.True(File.Exists(configFilePath));

            string contents = File.ReadAllText(configFilePath);
            Assert.NotEqual(-1, contents.IndexOf("repositoryformatversion = 0", StringComparison.Ordinal));
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
            Assert.NotNull(repo.Info.Path);
            Assert.True(repo.Info.IsEmpty);
            Assert.False(repo.Info.IsHeadDetached);
            Assert.True(repo.Info.IsHeadOrphaned);

            Reference headRef = repo.Refs["HEAD"];
            Assert.NotNull(headRef);
            Assert.Equal("refs/heads/master", headRef.TargetIdentifier);
            Assert.Null(headRef.ResolveToDirectReference());

            Assert.NotNull(repo.Head);
            Assert.True(repo.Head.IsCurrentRepositoryHead);
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
                Assert.NotNull(repo);
                Assert.Null(repo.Info.WorkingDirectory);
            }
        }

        [Fact]
        public void CanOpenStandardRepositoryThroughAWorkingDirPath()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.NotNull(repo);
                Assert.NotNull(repo.Info.WorkingDirectory);
            }
        }

        [Fact]
        public void OpeningStandardRepositoryThroughTheGitDirGuessesTheWorkingDirPath()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.NotNull(repo);
                Assert.NotNull(repo.Info.WorkingDirectory);
            }
        }

        [Fact]
        public void CanOpenRepository()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.NotNull(repo.Info.Path);
                Assert.Null(repo.Info.WorkingDirectory);
                Assert.True(repo.Info.IsBare);
                Assert.False(repo.Info.IsEmpty);
                Assert.False(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void OpeningNonExistentRepoThrows()
        {
            Assert.Throws<RepositoryNotFoundException>(() => { new Repository("a_bad_path"); });
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
                Assert.NotNull(gitObject);
                Assert.IsType<Commit>(gitObject);
            }
        }

        [Fact]
        public void CanLookupACommitByTheNameOfALightweightTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/tags/lw");
                Assert.NotNull(gitObject);
                Assert.IsType<Commit>(gitObject);
            }
        }

        [Fact]
        public void CanLookupATagAnnotationByTheNameOfAnAnnotatedTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject gitObject = repo.Lookup("refs/tags/e90810b");
                Assert.NotNull(gitObject);
                Assert.IsType<TagAnnotation>(gitObject);
            }
        }

        [Fact]
        public void CanLookupObjects()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.NotNull(repo.Lookup(commitSha));
                Assert.NotNull(repo.Lookup<Commit>(commitSha));
                Assert.NotNull(repo.Lookup<GitObject>(commitSha));
            }
        }

        [Fact]
        public void CanLookupSameObjectTwiceAndTheyAreEqual()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject commit = repo.Lookup(commitSha);
                GitObject commit2 = repo.Lookup(commitSha);
                Assert.True(commit.Equals(commit2));
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
                Assert.NotNull(repo.Lookup(commitSha));
                Assert.NotNull(repo.Lookup<Commit>(commitSha));
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
        public void CanLookupUsingRevparseSyntax()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Null(repo.Lookup<Tree>("master^"));

                Assert.NotNull(repo.Lookup("master:new.txt"));
                Assert.NotNull(repo.Lookup<Blob>("master:new.txt"));
                Assert.NotNull(repo.Lookup("master^"));
                Assert.NotNull(repo.Lookup<Commit>("master^"));
                Assert.NotNull(repo.Lookup("master~3"));
                Assert.NotNull(repo.Lookup("HEAD"));
                Assert.NotNull(repo.Lookup("refs/heads/br2"));
            }
        }

        [Fact]
        public void CanResolveAmbiguousRevparseSpecs()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var o1 = repo.Lookup("e90810b"); // This resolves to a tag
                Assert.Equal("7b4384978d2493e851f9cca7858815fac9b10980", o1.Sha);
                var o2 = repo.Lookup("e90810b8"); // This resolves to a commit
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", o2.Sha);
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
        public void LookingUpWithATooShortShaThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<AmbiguousException>(() => repo.Lookup("e90"));
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

        [Fact]
        public void CanDetectIfTheHeadIsOrphaned()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                string branchName = repo.Head.CanonicalName;

                Assert.False(repo.Info.IsHeadOrphaned);

                repo.Refs.Add("HEAD", "refs/heads/orphan", true);
                Assert.True(repo.Info.IsHeadOrphaned);

                repo.Refs.Add("HEAD", branchName, true);
                Assert.False(repo.Info.IsHeadOrphaned);
            }
        }
    }
}
