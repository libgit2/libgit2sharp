using System;
using System.Collections.Generic;
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
            string repoPath = InitNewRepository(true);

            using (var repo = new Repository(repoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));
                CheckGitConfigFile(dir);

                Assert.Null(repo.Info.WorkingDirectory);
                Assert.Equal(Path.GetFullPath(repoPath), repo.Info.Path);
                Assert.True(repo.Info.IsBare);

                AssertInitializedRepository(repo, "refs/heads/master");

                repo.Refs.Add("HEAD", "refs/heads/orphan", true);
                AssertInitializedRepository(repo, "refs/heads/orphan");
            }
        }

        [Fact]
        public void AccessingTheIndexInABareRepoThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Index);
            }
        }

        [Fact]
        public void CanCheckIfADirectoryLeadsToAValidRepository()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            Assert.False(Repository.IsValid(scd.DirectoryPath));

            Directory.CreateDirectory(scd.DirectoryPath);

            Assert.False(Repository.IsValid(scd.DirectoryPath));
        }

        [Fact]
        public void CanCreateStandardRepo()
        {
            string repoPath = InitNewRepository();

            Assert.True(Repository.IsValid(repoPath));

            using (var repo = new Repository(repoPath))
            {
                Assert.True(Repository.IsValid(repo.Info.WorkingDirectory));
                Assert.True(Repository.IsValid(repo.Info.Path));

                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));
                CheckGitConfigFile(dir);

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(repoPath, repo.Info.Path);
                Assert.False(repo.Info.IsBare);

                AssertIsHidden(repo.Info.Path);

                AssertInitializedRepository(repo, "refs/heads/master");

                repo.Refs.Add("HEAD", "refs/heads/orphan", true);
                AssertInitializedRepository(repo, "refs/heads/orphan");
            }
        }

        [Fact]
        public void CanCreateStandardRepoAndSpecifyAFolderWhichWillContainTheNewlyCreatedGitDirectory()
        {
            var scd1 = BuildSelfCleaningDirectory();
            var scd2 = BuildSelfCleaningDirectory();

            string repoPath = Repository.Init(scd1.DirectoryPath, scd2.DirectoryPath);

            Assert.True(Repository.IsValid(repoPath));

            using (var repo = new Repository(repoPath))
            {
                Assert.True(Repository.IsValid(repo.Info.WorkingDirectory));
                Assert.True(Repository.IsValid(repo.Info.Path));

                Assert.Equal(false, repo.Info.IsBare);

                char sep = Path.DirectorySeparatorChar;
                Assert.Equal(scd1.RootedDirectoryPath + sep, repo.Info.WorkingDirectory);
                Assert.Equal(scd2.RootedDirectoryPath + sep + ".git" + sep, repo.Info.Path);
            }
        }

        [Fact]
        public void CanCreateStandardRepoAndDirectlySpecifyAGitDirectory()
        {
            var scd1 = BuildSelfCleaningDirectory();
            var scd2 = BuildSelfCleaningDirectory();

            var gitDir = Path.Combine(scd2.DirectoryPath, ".git/");

            string repoPath = Repository.Init(scd1.DirectoryPath, gitDir);

            Assert.True(Repository.IsValid(repoPath));

            using (var repo = new Repository(repoPath))
            {
                Assert.True(Repository.IsValid(repo.Info.WorkingDirectory));
                Assert.True(Repository.IsValid(repo.Info.Path));

                Assert.Equal(false, repo.Info.IsBare);

                char sep = Path.DirectorySeparatorChar;
                Assert.Equal(scd1.RootedDirectoryPath + sep, repo.Info.WorkingDirectory);
                Assert.Equal(Path.GetFullPath(gitDir), repo.Info.Path);
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

        [Fact(Skip = "Skipping due to recent github handling modification of --include-tag.")]
        public void CanFetchFromRemoteByName()
        {
            string remoteName = "testRemote";
            string url = "http://github.com/libgit2/TestGitRepository";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // We will first fetch without specifying any Tag options.
                // After we verify this fetch, we will perform a second fetch
                // where we will download all tags, and verify that the
                // nearly-dangling tag is now present.

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo remoteInfo = TestRemoteInfo.TestRemoteInstance;
                ExpectedFetchState expectedFetchState = new ExpectedFetchState(remoteName);

                // Add expected branch objects
                foreach (KeyValuePair<string, ObjectId> kvp in remoteInfo.BranchTips)
                {
                    expectedFetchState.AddExpectedBranch(kvp.Key, ObjectId.Zero, kvp.Value);
                }

                // Add the expected tags
                string[] expectedTagNames = { "blob", "commit_tree", "annotated_tag" };
                foreach (string tagName in expectedTagNames)
                {
                    TestRemoteInfo.ExpectedTagInfo expectedTagInfo = remoteInfo.Tags[tagName];
                    expectedFetchState.AddExpectedTag(tagName, ObjectId.Zero, expectedTagInfo);
                }

                // Perform the actual fetch
                repo.Fetch(remote.Name, new FetchOptions { OnUpdateTips = expectedFetchState.RemoteUpdateTipsHandler });

                // Verify the expected state
                expectedFetchState.CheckUpdatedReferences(repo);

                // Now fetch the rest of the tags
                repo.Fetch(remote.Name, new FetchOptions { TagFetchMode = TagFetchMode.All });

                // Verify that the "nearly-dangling" tag is now in the repo.
                Tag nearlyDanglingTag = repo.Tags["nearly-dangling"];
                Assert.NotNull(nearlyDanglingTag);
                Assert.Equal(remoteInfo.Tags["nearly-dangling"].TargetId, nearlyDanglingTag.Target.Id);
            }
        }

        [Fact]
        public void CanReinitARepository()
        {
            string repoPath = InitNewRepository();

            using (var repository = new Repository(repoPath))
            {
                string repoPath2 = Repository.Init(repoPath, false);

                using (var repository2 = new Repository(repoPath2))
                {
                    Assert.Equal(repository2.Info.Path, repository.Info.Path);
                }
            }
        }

        [Fact]
        public void CreatingRepoWithBadParamsThrows()
        {
            Assert.Throws<ArgumentException>(() => Repository.Init(string.Empty));
            Assert.Throws<ArgumentNullException>(() => Repository.Init(null));
        }

        private static void AssertInitializedRepository(IRepository repo, string expectedHeadTargetIdentifier)
        {
            Assert.NotNull(repo.Info.Path);
            Assert.False(repo.Info.IsHeadDetached);
            Assert.True(repo.Info.IsHeadUnborn);

            Reference headRef = repo.Refs.Head;
            Assert.NotNull(headRef);
            Assert.Equal(expectedHeadTargetIdentifier, headRef.TargetIdentifier);
            Assert.Null(headRef.ResolveToDirectReference());

            Assert.NotNull(repo.Head);
            Assert.True(repo.Head.IsCurrentRepositoryHead);
            Assert.Equal(headRef.TargetIdentifier, repo.Head.CanonicalName);
            Assert.Null(repo.Head.Tip);

            Assert.Equal(0, repo.Commits.Count());
            Assert.Equal(0, repo.Commits.QueryBy(new CommitFilter()).Count());
            Assert.Equal(0, repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs.Head }).Count());
            Assert.Equal(0, repo.Commits.QueryBy(new CommitFilter { Since = repo.Head }).Count());
            Assert.Equal(0, repo.Commits.QueryBy(new CommitFilter { Since = "HEAD" }).Count());
            Assert.Equal(0, repo.Commits.QueryBy(new CommitFilter { Since = expectedHeadTargetIdentifier }).Count());

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

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                const string filename = "new.txt";
                Touch(repo.Info.WorkingDirectory, filename, "one ");
                repo.Index.Stage(filename);

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
                Assert.Throws<AmbiguousSpecificationException>(() => repo.Lookup("e90"));
            }
        }

        [Fact]
        public void LookingUpAGitLinkThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Lookup<GitLink>("e90810b"));
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string branchName = repo.Head.CanonicalName;

                Assert.False(repo.Info.IsHeadUnborn);

                repo.Refs.Add("HEAD", "refs/heads/orphan", true);
                Assert.True(repo.Info.IsHeadUnborn);

                repo.Refs.Add("HEAD", branchName, true);
                Assert.False(repo.Info.IsHeadUnborn);
            }
        }

        [Fact]
        public void QueryingTheRemoteForADetachedHeadBranchReturnsNull()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Checkout(repo.Head.Tip.Sha, CheckoutModifiers.Force, null, null);
                Branch trackLocal = repo.Head;
                Assert.Null(trackLocal.Remote);
            }
        }

        [Fact]
        public void ReadingEmptyRepositoryMessageReturnsNull()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.Null(repo.Info.Message);
            }
        }

        [Fact]
        public void CanReadRepositoryMessage()
        {
            string testMessage = "This is a test message!";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.Path, "MERGE_MSG", testMessage);

                Assert.Equal(testMessage, repo.Info.Message);
            }
        }

        [Fact]
        public void AccessingADeletedHeadThrows()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.NotNull(repo.Head);

                File.Delete(Path.Combine(repo.Info.Path, "HEAD"));

                Assert.Throws<LibGit2SharpException>(() => repo.Head);
            }
        }

        [Fact]
        public void CanDetectShallowness()
        {
            using (var repo = new Repository(ShallowTestRepoPath))
            {
                Assert.True(repo.Info.IsShallow);
            }

            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.False(repo.Info.IsShallow);
            }
        }
    }
}
