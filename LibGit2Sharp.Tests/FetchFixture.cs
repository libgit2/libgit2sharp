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

        [Theory(Skip = "Skipping due to recent github handling modification of --include-tag.")]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanFetchIntoAnEmptyRepository(string url)
        {
            using (var repo = InitIsolatedRepository())
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
                repo.Network.Fetch(remote, new FetchOptions { OnUpdateTips = expectedFetchState.RemoteUpdateTipsHandler });

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);
            }
        }

        [SkippableFact]
        public void CanFetchIntoAnEmptyRepositoryWithCredentials()
        {
            InconclusiveIf(() => string.IsNullOrEmpty(Constants.PrivateRepoUrl),
                "Populate Constants.PrivateRepo* to run this test");

            using (var repo = InitIsolatedRepository())
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, Constants.PrivateRepoUrl);

                // Perform the actual fetch
                repo.Network.Fetch(remote, new FetchOptions
                {
                    CredentialsProvider = Constants.PrivateRepoCredentials
                });
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanFetchAllTagsIntoAnEmptyRepository(string url)
        {
            using (var repo = InitIsolatedRepository())
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo remoteInfo = TestRemoteInfo.TestRemoteInstance;
                var expectedFetchState = new ExpectedFetchState(remoteName);

                // Add expected tags
                foreach (KeyValuePair<string, TestRemoteInfo.ExpectedTagInfo> kvp in remoteInfo.Tags)
                {
                    expectedFetchState.AddExpectedTag(kvp.Key, ObjectId.Zero, kvp.Value);
                }

                // Add expected branch objects
                foreach (KeyValuePair<string, ObjectId> kvp in remoteInfo.BranchTips)
                {
                    expectedFetchState.AddExpectedBranch(kvp.Key, ObjectId.Zero, kvp.Value);
                }

                // Perform the actual fetch
                repo.Network.Fetch(remote, new FetchOptions {
                    TagFetchMode = TagFetchMode.All,
                    OnUpdateTips = expectedFetchState.RemoteUpdateTipsHandler
                });

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);

                // Verify the reflog entries
                Assert.Equal(1, repo.Refs.Log(string.Format("refs/remotes/{0}/master", remoteName)).Count()); // Branches are also retrieved
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository", "test-branch", "master")]
        [InlineData("https://github.com/libgit2/TestGitRepository", "master", "master")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git", "master", "first-merge")]
        public void CanFetchCustomRefSpecsIntoAnEmptyRepository(string url, string localBranchName, string remoteBranchName)
        {
            using (var repo = InitIsolatedRepository())
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                string refSpec = string.Format("refs/heads/{2}:refs/remotes/{0}/{1}", remoteName, localBranchName, remoteBranchName);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo remoteInfo = TestRemoteInfo.TestRemoteInstance;
                var expectedFetchState = new ExpectedFetchState(remoteName);
                expectedFetchState.AddExpectedBranch(localBranchName, ObjectId.Zero, remoteInfo.BranchTips[remoteBranchName]);

                // Perform the actual fetch
                repo.Network.Fetch(remote, new string[] { refSpec }, new FetchOptions {
                    TagFetchMode = TagFetchMode.None,
                    OnUpdateTips = expectedFetchState.RemoteUpdateTipsHandler
                });

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);

                // Verify the reflog entries
                var reflogEntry = repo.Refs.Log(string.Format("refs/remotes/{0}/{1}", remoteName, localBranchName)).Single();
                Assert.True(reflogEntry.Message.StartsWith("fetch "));
            }
        }

        [Theory(Skip = "Skipping due to recent github handling modification of --include-tag.")]
        [InlineData(TagFetchMode.All, 4)]
        [InlineData(TagFetchMode.None, 0)]
        [InlineData(TagFetchMode.Auto, 3)]
        public void FetchRespectsConfiguredAutoTagSetting(TagFetchMode tagFetchMode, int expectedTagCount)
        {
            string url = "http://github.com/libgit2/TestGitRepository";

            using (var repo = InitIsolatedRepository())
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
