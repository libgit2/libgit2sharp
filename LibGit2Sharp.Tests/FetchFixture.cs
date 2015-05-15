using System;
using System.Collections.Generic;
using System.IO;
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
            string path = InitNewRepository();

            using (var repo = new Repository(path))
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

            string path = InitNewRepository();

            using (var repo = new Repository(path))
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
            string path = InitNewRepository();

            using (var repo = new Repository(path))
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
            string path = InitNewRepository();

            using (var repo = new Repository(path))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                string refSpec = string.Format("refs/heads/{2}:refs/remotes/{0}/{1}", remoteName, localBranchName, remoteBranchName);

                // Set up structures for the expected results
                // and verifying the RemoteUpdateTips callback.
                TestRemoteInfo remoteInfo = TestRemoteInfo.TestRemoteInstance;
                var expectedFetchState = new ExpectedFetchState(remoteName);
                expectedFetchState.AddExpectedBranch(localBranchName, ObjectId.Zero, remoteInfo.BranchTips[remoteBranchName]);

                // Let's account for opportunistic updates during the Fetch() call
                if (!string.Equals("master", localBranchName, StringComparison.OrdinalIgnoreCase))
                {
                    expectedFetchState.AddExpectedBranch("master", ObjectId.Zero, remoteInfo.BranchTips["master"]);
                }

                if (string.Equals("master", localBranchName, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals("master", remoteBranchName, StringComparison.OrdinalIgnoreCase))
                {
                    expectedFetchState.AddExpectedBranch(remoteBranchName, ObjectId.Zero, remoteInfo.BranchTips[remoteBranchName]);
                }

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

        [Theory]
        [InlineData(TagFetchMode.All, 4)]
        [InlineData(TagFetchMode.None, 0)]
        [InlineData(TagFetchMode.Auto, 3)]
        public void FetchRespectsConfiguredAutoTagSetting(TagFetchMode tagFetchMode, int expectedTagCount)
        {
            string url = "http://github.com/libgit2/TestGitRepository";

            string path = InitNewRepository();

            using (var repo = new Repository(path))
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

        [Fact]
        public void CanFetchAllTagsAfterAnInitialClone()
        {
            var scd = BuildSelfCleaningDirectory();

            const string url = "https://github.com/libgit2/TestGitRepository";

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                repo.Fetch("origin", new FetchOptions { TagFetchMode = TagFetchMode.All });
            }
        }

        [Fact]
        public void FetchHonorsTheFetchPruneConfigurationEntry()
        {
            var source = SandboxBareTestRepo();
            var url = new Uri(Path.GetFullPath(source)).AbsoluteUri;

            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            var options = BuildFakeConfigs(BuildSelfCleaningDirectory());

            using (var clonedRepo = new Repository(clonedRepoPath, options))
            {
                Assert.Equal(5, clonedRepo.Branches.Count(b => b.IsRemote));

                // Drop one of the branches in the remote repository
                using (var sourceRepo = new Repository(source))
                {
                    sourceRepo.Branches.Remove("packed-test");
                }

                // No pruning when the configuration entry isn't defined
                Assert.Null(clonedRepo.Config.Get<bool>("fetch.prune"));
                clonedRepo.Fetch("origin");
                Assert.Equal(5, clonedRepo.Branches.Count(b => b.IsRemote));

                // No pruning when the configuration entry is set to false
                clonedRepo.Config.Set<bool>("fetch.prune", false);
                clonedRepo.Fetch("origin");
                Assert.Equal(5, clonedRepo.Branches.Count(b => b.IsRemote));

                // Auto pruning when the configuration entry is set to true
                clonedRepo.Config.Set<bool>("fetch.prune", true);
                clonedRepo.Fetch("origin");
                Assert.Equal(4, clonedRepo.Branches.Count(b => b.IsRemote));
            }
        }
    }
}
