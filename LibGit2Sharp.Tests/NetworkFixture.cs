using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class NetworkFixture : BaseFixture
    {
        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanListRemoteReferences(string url)
        {
            string remoteName = "testRemote";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);
                IList<DirectReference> references = repo.Network.ListReferences(remote).ToList();

                foreach (var directReference in references)
                {
                    // None of those references point to an existing
                    // object in this brand new repository
                    Assert.Null(directReference.Target);
                }

                List<Tuple<string, string>> actualRefs = references.
                    Select(directRef => new Tuple<string, string>(directRef.CanonicalName, directRef.TargetIdentifier)).ToList();

                Assert.Equal(ExpectedRemoteRefs.Count, actualRefs.Count);
                for (int i = 0; i < ExpectedRemoteRefs.Count; i++)
                {
                    Assert.Equal(ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                    Assert.Equal(ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
                }
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanListRemoteReferencesFromUrl(string url)
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                IList<DirectReference> references = repo.Network.ListReferences(url).ToList();

                foreach (var directReference in references)
                {
                    // None of those references point to an existing
                    // object in this brand new repository
                    Assert.Null(directReference.Target);
                }

                List<Tuple<string, string>> actualRefs = references.
                    Select(directRef => new Tuple<string, string>(directRef.CanonicalName, directRef.TargetIdentifier)).ToList();

                Assert.Equal(ExpectedRemoteRefs.Count, actualRefs.Count);
                for (int i = 0; i < ExpectedRemoteRefs.Count; i++)
                {
                    Assert.Equal(ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                    Assert.Equal(ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
                }
            }
        }

        [Fact]
        public void CanListRemoteReferenceObjects()
        {
            const string url = "http://github.com/libgit2/TestGitRepository";
            const string remoteName = "origin";

            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                Remote remote = repo.Network.Remotes[remoteName];
                IEnumerable<DirectReference> references = repo.Network.ListReferences(remote);

                var actualRefs = new List<Tuple<string,string>>();

                foreach(DirectReference reference in references)
                {
                    Assert.NotNull(reference.CanonicalName);
                    Assert.NotNull(reference.Target);
                    actualRefs.Add(new Tuple<string, string>(reference.CanonicalName, reference.Target.Id.Sha));
                }

                Assert.Equal(ExpectedRemoteRefs.Count, actualRefs.Count);
                for (int i = 0; i < ExpectedRemoteRefs.Count; i++)
                {
                    Assert.Equal(ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
                    Assert.Equal(ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                }
            }
        }

        [Theory]
        [InlineData(FastForwardStrategy.Default)]
        [InlineData(FastForwardStrategy.NoFastFoward)]
        public void CanPull(FastForwardStrategy fastForwardStrategy)
        {
            string url = "https://github.com/libgit2/TestGitRepository";

            var scd = BuildSelfCleaningDirectory();
            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                repo.Reset(ResetMode.Hard, "HEAD~1");

                Assert.False(repo.Index.RetrieveStatus().Any());
                Assert.Equal(repo.Lookup<Commit>("refs/remotes/origin/master~1"), repo.Head.Tip);

                PullOptions pullOptions = new PullOptions()
                {
                    MergeOptions = new MergeOptions()
                    {
                        FastForwardStrategy = fastForwardStrategy
                    }
                };

                MergeResult mergeResult = repo.Network.Pull(Constants.Signature, pullOptions);

                if(fastForwardStrategy == FastForwardStrategy.Default || fastForwardStrategy == FastForwardStrategy.FastForwardOnly)
                {
                    Assert.Equal(mergeResult.Status, MergeStatus.FastForward);
                    Assert.Equal(mergeResult.Commit, repo.Branches["refs/remotes/origin/master"].Tip);
                    Assert.Equal(repo.Head.Tip, repo.Branches["refs/remotes/origin/master"].Tip);
                }
                else
                {
                    Assert.Equal(mergeResult.Status, MergeStatus.NonFastForward);
                }
            }
        }

        [Fact]
        public void CanPullIntoEmptyRepo()
        {
            string url = "https://github.com/libgit2/TestGitRepository";
            string remoteName = "origin";
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                // Set up remote
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up tracking information
                repo.Branches.Update(repo.Head,
                    b => b.Remote = remoteName,
                    b => b.UpstreamBranch = "refs/heads/master");

                // Pull!
                MergeResult mergeResult = repo.Network.Pull(Constants.Signature, new PullOptions());

                Assert.Equal(mergeResult.Status, MergeStatus.FastForward);
                Assert.Equal(mergeResult.Commit, repo.Branches["refs/remotes/origin/master"].Tip);
                Assert.Equal(repo.Head.Tip, repo.Branches["refs/remotes/origin/master"].Tip);
            }
        }

        /*
        * git ls-remote http://github.com/libgit2/TestGitRepository
        * 49322bb17d3acc9146f98c97d078513228bbf3c0        HEAD
        * 0966a434eb1a025db6b71485ab63a3bfbea520b6        refs/heads/first-merge
        * 49322bb17d3acc9146f98c97d078513228bbf3c0        refs/heads/master
        * 42e4e7c5e507e113ebbb7801b16b52cf867b7ce1        refs/heads/no-parent
        * d96c4e80345534eccee5ac7b07fc7603b56124cb        refs/tags/annotated_tag
        * c070ad8c08840c8116da865b2d65593a6bb9cd2a        refs/tags/annotated_tag^{}
        * 55a1a760df4b86a02094a904dfa511deb5655905        refs/tags/blob
        * 8f50ba15d49353813cc6e20298002c0d17b0a9ee        refs/tags/commit_tree
        * 6e0c7bdb9b4ed93212491ee778ca1c65047cab4e        refs/tags/nearly-dangling
        */
        /// <summary>
        /// Expected references on http://github.com/libgit2/TestGitRepository
        /// </summary>
        private static List<Tuple<string, string>> ExpectedRemoteRefs = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("HEAD", "49322bb17d3acc9146f98c97d078513228bbf3c0"),
            new Tuple<string, string>("refs/heads/first-merge", "0966a434eb1a025db6b71485ab63a3bfbea520b6"),
            new Tuple<string, string>("refs/heads/master", "49322bb17d3acc9146f98c97d078513228bbf3c0"),
            new Tuple<string, string>("refs/heads/no-parent", "42e4e7c5e507e113ebbb7801b16b52cf867b7ce1"),
            new Tuple<string, string>("refs/tags/annotated_tag", "d96c4e80345534eccee5ac7b07fc7603b56124cb"),
            new Tuple<string, string>("refs/tags/annotated_tag^{}", "c070ad8c08840c8116da865b2d65593a6bb9cd2a"),
            new Tuple<string, string>("refs/tags/blob", "55a1a760df4b86a02094a904dfa511deb5655905"),
            new Tuple<string, string>("refs/tags/commit_tree", "8f50ba15d49353813cc6e20298002c0d17b0a9ee"),
            new Tuple<string, string>("refs/tags/nearly-dangling", "6e0c7bdb9b4ed93212491ee778ca1c65047cab4e"),
        };
    }
}
