using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class FetchHeadFixture : BaseFixture
    {
        [Fact]
        public void FetchHeadIsEmptyByDefault()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(0, repo.Network.FetchHeads.Count());
            }
        }

        private class FetchHeadExpected
        {
            public String Name { get; set; }
            public String RemoteName { get; set; }
            public String Url { get; set; }
            public string TargetSha { get; set; }
            public bool ForMerge { get; set; }
        }

        [Theory]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanIterateFetchHead(string url)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                repo.Reset(ResetOptions.Hard, "HEAD~2");

                // Create a file, stage it, and commit it.
                const string filename = "b.txt";
                Touch(repo.Info.WorkingDirectory, filename);
                repo.Index.Stage(filename);
                repo.Commit("comment", Constants.Signature, Constants.Signature);

                // Issue the fetch.
                repo.Fetch("origin");

                // Retrieve the fetch heads and verify the expected result.
                var expected = new[] {
                    new FetchHeadExpected
                    {
                        Name = "FETCH_HEAD[0]",
                        RemoteName = "refs/heads/master",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        TargetSha = "49322bb17d3acc9146f98c97d078513228bbf3c0",
                        ForMerge = true,
                    },
                    new FetchHeadExpected
                    {
                        Name = "FETCH_HEAD[1]",
                        RemoteName = "refs/heads/first-merge",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        TargetSha = "0966a434eb1a025db6b71485ab63a3bfbea520b6",
                        ForMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "FETCH_HEAD[2]",
                        RemoteName = "refs/heads/no-parent",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        TargetSha = "42e4e7c5e507e113ebbb7801b16b52cf867b7ce1",
                        ForMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "FETCH_HEAD[3]",
                        RemoteName = "refs/tags/annotated_tag",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        TargetSha = "d96c4e80345534eccee5ac7b07fc7603b56124cb",
                        ForMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "FETCH_HEAD[4]",
                        RemoteName = "refs/tags/blob",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        TargetSha = "55a1a760df4b86a02094a904dfa511deb5655905",
                        ForMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "FETCH_HEAD[5]",
                        RemoteName = "refs/tags/commit_tree",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        TargetSha = "8f50ba15d49353813cc6e20298002c0d17b0a9ee",
                        ForMerge = false,
                    },
                     new FetchHeadExpected
                    {
                        Name = "FETCH_HEAD[6]",
                        RemoteName = "refs/tags/nearly-dangling",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        TargetSha = "6e0c7bdb9b4ed93212491ee778ca1c65047cab4e",
                        ForMerge = false,
                    },
                };

                VerifyFetchHead(repo.Network.FetchHeads.ToArray(), expected);
            }
        }

        private static void VerifyFetchHead(FetchHead[] actual, FetchHeadExpected[] expected)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);

            Assert.Equal(actual.Count(), expected.Count());

            int i = 0;
            foreach (FetchHead fetchHead in actual)
            {
                Assert.Equal(fetchHead.CanonicalName, expected[i].Name);
                Assert.Equal(fetchHead.RemoteCanonicalName, expected[i].RemoteName);
                Assert.Equal(fetchHead.Url, expected[i].Url);
                Assert.Equal(fetchHead.Target.Sha, expected[i].TargetSha);
                Assert.Equal(fetchHead.ForMerge, expected[i].ForMerge);

                i++;
            }
        }
    }
}
