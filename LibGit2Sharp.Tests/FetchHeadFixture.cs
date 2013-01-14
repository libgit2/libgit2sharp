using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class FetchHeadFixture : BaseFixture
    {
        [Fact]
        public void FetchHeadIsEmptyByDefault()
        {
            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                Assert.Equal(0, repo.FetchHead.Count());
            }
        }

        private class FetchHeadExpected
        {
            public String Name { get; set; }
            public String Url { get; set; }
            public ObjectId CommitId { get; set; }
            public bool IsMerge { get; set; }
        }

        [Theory]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanIterateFetchHead(string url)
        {
            var scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Clone(url, scd.RootedDirectoryPath))
            {
                repo.Reset(ResetOptions.Hard, "HEAD~2");

                // Create a file, stage it, and commit it.
                String filename = "b.txt";
                String fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                File.WriteAllText(fullPath, "");
                repo.Index.Stage(filename);
                repo.Commit("comment", Constants.Signature, Constants.Signature);

                // Issue the fetch.
                repo.Fetch("origin");

                // Retrieve the fetch heads and verify the expected result.
                FetchHeadExpected[] expected = new FetchHeadExpected[] {
                    new FetchHeadExpected
                    {
                        Name = "refs/heads/master",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        CommitId = new ObjectId("49322bb17d3acc9146f98c97d078513228bbf3c0"),
                        IsMerge = true,
                    },
                    new FetchHeadExpected
                    {
                        Name = "refs/heads/first-merge",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        CommitId = new ObjectId("0966a434eb1a025db6b71485ab63a3bfbea520b6"),
                        IsMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "refs/heads/no-parent",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        CommitId = new ObjectId("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1"),
                        IsMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "refs/tags/annotated_tag",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        CommitId = new ObjectId("d96c4e80345534eccee5ac7b07fc7603b56124cb"),
                        IsMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "refs/tags/blob",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        CommitId = new ObjectId("55a1a760df4b86a02094a904dfa511deb5655905"),
                        IsMerge = false,
                    },
                    new FetchHeadExpected
                    {
                        Name = "refs/tags/commit_tree",
                        Url = "git://github.com/libgit2/TestGitRepository.git",
                        CommitId = new ObjectId("8f50ba15d49353813cc6e20298002c0d17b0a9ee"),
                        IsMerge = false,
                    },
                };

                VerifyFetchHead(repo.FetchHead, expected);
            }
        }

        private static void VerifyFetchHead(FetchHeadCollection actual, FetchHeadExpected[] expected)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);

            Assert.Equal(actual.Count(), expected.Count());

            int i = 0;
            foreach (FetchHead fetchHead in actual)
            {
                Assert.Equal(fetchHead.Name, expected[i].Name);
                Assert.Equal(fetchHead.Url, expected[i].Url);
                Assert.Equal(fetchHead.CommitId.Sha, expected[i].CommitId.Sha);
                Assert.Equal(fetchHead.IsMerge, expected[i].IsMerge);

                i++;
            }
        }
    }
}
