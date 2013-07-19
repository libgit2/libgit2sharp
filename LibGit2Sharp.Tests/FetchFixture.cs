using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class FetchFixture : BaseFixture
    {
        private const string remoteName = "testRemote";

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanFetchIntoAnEmptyRepository(string url)
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo expectedResults = TestRemoteInfo.TestRemoteInstance;
                var expectedFetchState = new ExpectedFetchState(remoteName);

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
                repo.Network.Fetch(remote, onUpdateTips: expectedFetchState.RemoteUpdateTipsHandler);

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);
            }
        }

        [SkippableFact]
        public void CanFetchIntoAnEmptyRepositoryWithCredentials()
        {
            InconclusiveIf(() => string.IsNullOrEmpty(Constants.PrivateRepoUrl),
                "Populate Constants.PrivateRepo* to run this test");

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, Constants.PrivateRepoUrl);

                // Perform the actual fetch
                repo.Network.Fetch(remote, credentials: new Credentials
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
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo remoteInfo = TestRemoteInfo.TestRemoteInstance;
                var expectedFetchState = new ExpectedFetchState(remoteName);

                // Add expected tags only as no branches are expected to be fetched
                foreach (KeyValuePair<string, TestRemoteInfo.ExpectedTagInfo> kvp in remoteInfo.Tags)
                {
                    expectedFetchState.AddExpectedTag(kvp.Key, ObjectId.Zero, kvp.Value);
                }

                // Perform the actual fetch
                repo.Network.Fetch(remote, TagFetchMode.All, onUpdateTips: expectedFetchState.RemoteUpdateTipsHandler);

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);
            }
        }

        [Theory]
        [InlineData(TagFetchMode.All, 4)]
        [InlineData(TagFetchMode.None, 0)]
        [InlineData(TagFetchMode.Auto, 3)]
        public void FetchRespectsConfiguredAutoTagSetting(TagFetchMode tagFetchMode, int expectedTagCount)
        {
            string url = "http://github.com/libgit2/TestGitRepository";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);
                Assert.NotNull(remote);

                // Update the configured autotag setting.
                repo.Network.Remotes.Update(remote,
                    r => r.TagFetchMode = tagFetchMode);

                // Perform the actual fetch.
                repo.Network.Fetch(remote);

                // Verify the number of fetched tags.
                Assert.Equal(expectedTagCount, repo.Tags.Count());
            }
        }
    }
}
