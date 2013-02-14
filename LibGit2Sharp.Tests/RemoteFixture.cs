using System.Collections.Generic;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RemoteFixture : BaseFixture
    {
        [Fact]
        public void CanGetRemoteOrigin()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Remote origin = repo.Network.Remotes["origin"];
                Assert.NotNull(origin);
                Assert.Equal("origin", origin.Name);
                Assert.Equal("c:/GitHub/libgit2sharp/Resources/testrepo.git", origin.Url);
            }
        }

        [Fact]
        public void GettingRemoteThatDoesntExistReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Network.Remotes["test"]);
            }
        }

        [Fact]
        public void CanEnumerateTheRemotes()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                int count = 0;

                foreach (Remote remote in repo.Network.Remotes)
                {
                    Assert.NotNull(remote);
                    count++;
                }

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void CanCheckEqualityOfRemote()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                Remote oneOrigin = repo.Network.Remotes["origin"];
                Assert.NotNull(oneOrigin);

                Remote otherOrigin = repo.Network.Remotes["origin"];
                Assert.Equal(oneOrigin, otherOrigin);

                Remote createdRemote = repo.Network.Remotes.Add("origin2", oneOrigin.Url);

                Remote loadedRemote = repo.Network.Remotes["origin2"];
                Assert.NotNull(loadedRemote);
                Assert.Equal(createdRemote, loadedRemote);

                Assert.NotEqual(oneOrigin, loadedRemote);
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanFetchIntoAnEmptyRepository(string url)
        {
            string remoteName = "testRemote";

            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo expectedResults = TestRemoteInfo.TestRemoteInstance;
                ExpectedFetchState expectedFetchState = new ExpectedFetchState(remoteName);

                // Add expected branch objects
                foreach (KeyValuePair<string, ObjectId> kvp in expectedResults.BranchTips)
                {
                    expectedFetchState.AddExpectedBranch(kvp.Key, ObjectId.Zero, kvp.Value);
                }

                // Add the expected tags
                string[] expectedTagNames = { "blob", "commit_tree", "annotated_tag" };
                foreach (string tagName in expectedTagNames)
                {
                    TestRemoteInfo.ExpectedTagInfo expectedTagInfo = expectedResults.Tags[tagName];
                    expectedFetchState.AddExpectedTag(tagName, ObjectId.Zero, expectedTagInfo);
                }

                // Perform the actual fetch
                remote.Fetch(onUpdateTips: expectedFetchState.RemoteUpdateTipsHandler);

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);
            }
        }

        [SkippableFact]
        public void CanFetchIntoAnEmptyRepositoryWithCredentials()
        {
            InconclusiveIf(() => string.IsNullOrEmpty(Constants.PrivateRepoUrl),
                "Populate Constants.PrivateRepo* to run this test");

            string remoteName = "testRemote";

            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, Constants.PrivateRepoUrl);

                // Perform the actual fetch
                remote.Fetch(credentials: new Credentials
                                              {
                                                  Username = Constants.PrivateRepoUsername,
                                                  Password = Constants.PrivateRepoPassword
                                              });
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanFetchAllTagsIntoAnEmptyRepository(string url)
        {
            string remoteName = "testRemote";

            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo remoteInfo = TestRemoteInfo.TestRemoteInstance;
                ExpectedFetchState expectedFetchState = new ExpectedFetchState(remoteName);

                // Add expected branch objects
                foreach (KeyValuePair<string, ObjectId> kvp in remoteInfo.BranchTips)
                {
                    expectedFetchState.AddExpectedBranch(kvp.Key, ObjectId.Zero, kvp.Value);
                }

                // Add expected tags
                foreach (KeyValuePair<string, TestRemoteInfo.ExpectedTagInfo> kvp in remoteInfo.Tags)
                {
                    expectedFetchState.AddExpectedTag(kvp.Key, ObjectId.Zero, kvp.Value);
                }

                // Perform the actual fetch
                remote.Fetch(TagFetchMode.All, onUpdateTips: expectedFetchState.RemoteUpdateTipsHandler);

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);
            }
        }

        [Fact]
        public void CreatingANewRemoteAddsADefaultRefSpec()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "upstream";
                const string url = "https://github.com/libgit2/libgit2sharp.git";

                repo.Network.Remotes.Add(name, url);
                Remote remote = repo.Network.Remotes[name];
                Assert.NotNull(remote);

                Assert.Equal(name, remote.Name);
                Assert.Equal(url, remote.Url);

                var refSpec = repo.Config.Get<string>("remote", remote.Name, "fetch");
                Assert.NotNull(refSpec);

                Assert.Equal("+refs/heads/*:refs/remotes/upstream/*", refSpec.Value);
            }
        }

        [Fact]
        public void CanAddANewRemoteWithAFetchRefSpec()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "pull-requests";
                const string url = "https://github.com/libgit2/libgit2sharp.git";
                const string fetchRefSpec = "+refs/pull/*:refs/remotes/pull-requests/*";

                repo.Network.Remotes.Add(name, url, fetchRefSpec);

                var refSpec = repo.Config.Get<string>("remote", name, "fetch");
                Assert.NotNull(refSpec);

                Assert.Equal(fetchRefSpec, refSpec.Value);
            }
        }

        [Theory]
        [InlineData("sher.lock")]
        [InlineData("/")]
        public void AddingARemoteWithAnInvalidNameThrows(string name)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                const string url = "https://github.com/libgit2/libgit2sharp.git";

                Assert.Throws<InvalidSpecificationException>(() => repo.Network.Remotes.Add(name, url));
            }
        }

        [Theory]
        [InlineData("valid/remote", true)]
        [InlineData("sher.lock", false)]
        [InlineData("/", false)]
        public void CanTellIfARemoteNameIsValid(string refname, bool expectedResult)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedResult, repo.Network.Remotes.IsValidName(refname));
            }
        }
    }
}
