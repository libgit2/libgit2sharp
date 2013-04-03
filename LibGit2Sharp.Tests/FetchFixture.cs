using System.Collections.Generic;
using LibGit2Sharp.Tests.TestHelpers;
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
            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.RootedDirectoryPath))
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

            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.RootedDirectoryPath))
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
            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo remoteInfo = TestRemoteInfo.TestRemoteInstance;
                var expectedFetchState = new ExpectedFetchState(remoteName);

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
                repo.Network.Fetch(remote, TagFetchMode.All, onUpdateTips: expectedFetchState.RemoteUpdateTipsHandler);

                // Verify the expected
                expectedFetchState.CheckUpdatedReferences(repo);
            }
        }
    }
}
