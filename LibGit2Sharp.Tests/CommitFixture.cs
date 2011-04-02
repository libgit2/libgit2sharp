using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class CommitFixture
    {
        private const string sha = "8496071c1b46c854b31185ea97743be6a8774479";
        private readonly List<string> expectedShas = new List<string> {"a4a7d", "c4780", "9fd73", "4a202", "5b5b0", "84960"};

        [Test]
        public void CanEnumerateCommits()
        {
            int count = 0;
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var commit in repo.Commits)
                {
                    commit.ShouldNotBeNull();
                    count++;
                }
            }
            count.ShouldEqual(6);
        }

        [Test]
        public void CanEnumerateCommitsFromSha()
        {
            int count = 0;
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var commit in repo.Commits.StartingAt("a4a7dce85cf63874e984719f4fdd239f5145052f"))
                {
                    commit.ShouldNotBeNull();
                    count++;
                }
            }
            count.ShouldEqual(6);
        }

        [Test]
        public void CanEnumerateCommitsWithReverseTimeSorting()
        {
            expectedShas.Reverse();
            int count = 0;
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var commit in repo.Commits.StartingAt("a4a7dce85cf63874e984719f4fdd239f5145052f").SortBy(GitSortOptions.Time | GitSortOptions.Reverse))
                {
                    commit.ShouldNotBeNull();
                    commit.Sha.StartsWith(expectedShas[count]);
                    count++;
                }
            }
            count.ShouldEqual(6);
        }

        [Test]
        public void CanEnumerateCommitsWithReverseTopoSorting()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commits = repo.Commits.StartingAt("a4a7dce85cf63874e984719f4fdd239f5145052f").SortBy(GitSortOptions.Topo | GitSortOptions.Reverse).ToList();
                foreach (var commit in commits)
                {
                    commit.ShouldNotBeNull();
                    foreach (var p in commit.Parents)
                    {
                        var parent = commits.Single(x => x.Id == p.Id);
                        Assert.Greater(commits.IndexOf(commit), commits.IndexOf(parent));
                    }
                }
            }
        }

        [Test]
        public void CanEnumerateCommitsWithTimeSorting()
        {
            int count = 0;
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var commit in repo.Commits.StartingAt("a4a7dce85cf63874e984719f4fdd239f5145052f").SortBy(GitSortOptions.Time))
                {
                    commit.ShouldNotBeNull();
                    commit.Sha.StartsWith(expectedShas[count]);
                    count++;
                }
            }
            count.ShouldEqual(6);
        }

        [Test]
        public void CanEnumerateCommitsWithTopoSorting()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commits = repo.Commits.StartingAt("a4a7dce85cf63874e984719f4fdd239f5145052f").SortBy(GitSortOptions.Topo).ToList();
                foreach (var commit in commits)
                {
                    commit.ShouldNotBeNull();
                    foreach (var p in commit.Parents)
                    {
                        var parent = commits.Single(x => x.Id == p.Id);
                        Assert.Less(commits.IndexOf(commit), commits.IndexOf(parent));
                    }
                }
            }
        }

        [Test]
        public void CanLookupCommitAlt()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commit = repo.Commits[sha];
                commit.Message.ShouldEqual("testing\n");
                commit.MessageShort.ShouldEqual("testing");
                commit.Sha.ShouldEqual(sha);
            }
        }

        [Test]
        public void CanLookupCommitGeneric()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commit = repo.Lookup<Commit>(sha);
                commit.Message.ShouldEqual("testing\n");
                commit.MessageShort.ShouldEqual("testing");
                commit.Sha.ShouldEqual(sha);
            }
        }

        [Test]
        public void CanReadCommitData()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var obj = repo.Lookup(sha);
                obj.ShouldNotBeNull();
                obj.GetType().ShouldEqual(typeof (Commit));

                var commit = (Commit) obj;
                commit.Message.ShouldEqual("testing\n");
                commit.MessageShort.ShouldEqual("testing");
                commit.Sha.ShouldEqual(sha);

                commit.Author.ShouldNotBeNull();
                commit.Author.Name.ShouldEqual("Scott Chacon");
                commit.Author.Email.ShouldEqual("schacon@gmail.com");
                commit.Author.When.ToSecondsSinceEpoch().ShouldEqual(1273360386);

                commit.Committer.ShouldNotBeNull();
                commit.Committer.Name.ShouldEqual("Scott Chacon");
                commit.Committer.Email.ShouldEqual("schacon@gmail.com");
                commit.Committer.When.ToSecondsSinceEpoch().ShouldEqual(1273360386);

                commit.Parents.Count.ShouldEqual(0);
            }
        }

        [Test]
        public void CanReadCommitWithMultipleParents()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commit = repo.Lookup<Commit>("a4a7dce85cf63874e984719f4fdd239f5145052f");
                commit.Parents.Count.ShouldEqual(2);
            }
        }

        [Test]
        public void PushingEmptyShaThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Commits.StartingAt(string.Empty));
            }
        }

        [Test]
        public void PushingNullShaThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Commits.StartingAt((string) null));
            }
        }
    }
}