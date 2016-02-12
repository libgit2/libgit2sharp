using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CommitFixture : BaseFixture
    {
        private const string sha = "8496071c1b46c854b31185ea97743be6a8774479";
        private readonly List<string> expectedShas = new List<string> { "a4a7d", "c4780", "9fd73", "4a202", "5b5b0", "84960" };

        [Fact]
        public void CanCountCommits()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(7, repo.Commits.Count());
            }
        }

        [Fact]
        public void CanCorrectlyCountCommitsWhenSwitchingToAnotherBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Hard reset and then remove untracked files
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                repo.Checkout("test");
                Assert.Equal(2, repo.Commits.Count());
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", repo.Commits.First().Id.Sha);

                repo.Checkout("master");
                Assert.Equal(9, repo.Commits.Count());
                Assert.Equal("32eab9cb1f450b5fe7ab663462b77d7f4b703344", repo.Commits.First().Id.Sha);
            }
        }

        [Fact]
        public void CanEnumerateCommits()
        {
            int count = 0;
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                foreach (Commit commit in repo.Commits)
                {
                    Assert.NotNull(commit);
                    count++;
                }
            }
            Assert.Equal(7, count);
        }

        [Fact]
        public void CanEnumerateCommitsInDetachedHeadState()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                ObjectId parentOfHead = repo.Head.Tip.Parents.First().Id;

                repo.Refs.Add("HEAD", parentOfHead.Sha, true);
                Assert.Equal(true, repo.Info.IsHeadDetached);

                Assert.Equal(6, repo.Commits.Count());
            }
        }

        [Fact]
        public void DefaultOrderingWhenEnumeratingCommitsIsTimeBased()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(CommitSortStrategies.Time, repo.Commits.SortedBy);
            }
        }

        [Fact]
        public void CanEnumerateCommitsFromSha()
        {
            int count = 0;
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                foreach (Commit commit in repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = "a4a7dce85cf63874e984719f4fdd239f5145052f" }))
                {
                    Assert.NotNull(commit);
                    count++;
                }
            }
            Assert.Equal(6, count);
        }

        [Fact]
        public void QueryingTheCommitHistoryWithUnknownShaOrInvalidEntryPointThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = Constants.UnknownSha }).Count());
                Assert.Throws<NotFoundException>(() => repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = "refs/heads/deadbeef" }).Count());
                Assert.Throws<ArgumentNullException>(() => repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = null }).Count());
            }
        }

        [Fact]
        public void QueryingTheCommitHistoryFromACorruptedReferenceThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                CreateCorruptedDeadBeefHead(repo.Info.Path);

                Assert.Throws<NotFoundException>(() => repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = repo.Branches["deadbeef"] }).Count());
                Assert.Throws<NotFoundException>(() => repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = repo.Refs["refs/heads/deadbeef"] }).Count());
            }
        }

        [Fact]
        public void QueryingTheCommitHistoryWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = string.Empty }));
                Assert.Throws<ArgumentNullException>(() => repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = null }));
                Assert.Throws<ArgumentNullException>(() => repo.Commits.QueryBy(default(CommitFilter)));
            }
        }

        [Fact]
        public void CanEnumerateCommitsWithReverseTimeSorting()
        {
            var reversedShas = new List<string>(expectedShas);
            reversedShas.Reverse();

            int count = 0;
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                foreach (Commit commit in repo.Commits.QueryBy(new CommitFilter
                                                                    {
                                                                        IncludeReachableFrom = "a4a7dce85cf63874e984719f4fdd239f5145052f",
                                                                        SortBy = CommitSortStrategies.Time | CommitSortStrategies.Reverse
                                                                    }))
                {
                    Assert.NotNull(commit);
                    Assert.True(commit.Sha.StartsWith(reversedShas[count]));
                    count++;
                }
            }
            Assert.Equal(6, count);
        }

        [Fact]
        public void CanEnumerateCommitsWithReverseTopoSorting()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                List<Commit> commits = repo.Commits.QueryBy(new CommitFilter
                                                                {
                                                                    IncludeReachableFrom = "a4a7dce85cf63874e984719f4fdd239f5145052f",
                                                                    SortBy = CommitSortStrategies.Time | CommitSortStrategies.Reverse
                                                                }).ToList();
                foreach (Commit commit in commits)
                {
                    Assert.NotNull(commit);
                    foreach (Commit p in commit.Parents)
                    {
                        Commit parent = commits.Single(x => x.Id == p.Id);
                        Assert.True(commits.IndexOf(commit) > commits.IndexOf(parent));
                    }
                }
            }
        }

        [Fact]
        public void CanSimplifyByFirstParent()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = repo.Head, FirstParentOnly = true },
            new[]
            {
                "4c062a6", "be3563a", "9fd738e",
                "4a202b3", "5b5b025", "8496071",
            });
        }

        [Fact]
        public void CanGetParentsCount()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(1, repo.Commits.First().Parents.Count());
            }
        }

        [Fact]
        public void CanEnumerateCommitsWithTimeSorting()
        {
            int count = 0;
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                foreach (Commit commit in repo.Commits.QueryBy(new CommitFilter
                                                                    {
                                                                        IncludeReachableFrom = "a4a7dce85cf63874e984719f4fdd239f5145052f",
                                                                        SortBy = CommitSortStrategies.Time
                                                                    }))
                {
                    Assert.NotNull(commit);
                    Assert.True(commit.Sha.StartsWith(expectedShas[count]));
                    count++;
                }
            }
            Assert.Equal(6, count);
        }

        [Fact]
        public void CanEnumerateCommitsWithTopoSorting()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                List<Commit> commits = repo.Commits.QueryBy(new CommitFilter
                                                                {
                                                                    IncludeReachableFrom = "a4a7dce85cf63874e984719f4fdd239f5145052f",
                                                                    SortBy = CommitSortStrategies.Topological
                                                                }).ToList();
                foreach (Commit commit in commits)
                {
                    Assert.NotNull(commit);
                    foreach (Commit p in commit.Parents)
                    {
                        Commit parent = commits.Single(x => x.Id == p.Id);
                        Assert.True(commits.IndexOf(commit) < commits.IndexOf(parent));
                    }
                }
            }
        }

        [Fact]
        public void CanEnumerateFromHead()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = repo.Head },
                new[]
                    {
                        "4c062a6", "be3563a", "c47800c", "9fd738e",
                        "4a202b3", "5b5b025", "8496071",
                    });
        }

        [Fact]
        public void CanEnumerateFromDetachedHead()
        {
            string path = SandboxStandardTestRepo();
            using (var repoClone = new Repository(path))
            {
                // Hard reset and then remove untracked files
                repoClone.Reset(ResetMode.Hard);
                repoClone.RemoveUntrackedFiles();

                string headSha = repoClone.Head.Tip.Sha;
                repoClone.Checkout(headSha);

                AssertEnumerationOfCommitsInRepo(repoClone,
                    repo => new CommitFilter { IncludeReachableFrom = repo.Head },
                    new[]
                        {
                            "32eab9c", "592d3c8", "4c062a6",
                            "be3563a", "c47800c", "9fd738e",
                            "4a202b3", "5b5b025", "8496071",
                        });
            }
        }

        [Fact]
        public void CanEnumerateUsingTwoHeadsAsBoundaries()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = "HEAD", ExcludeReachableFrom = "refs/heads/br2" },
                new[] { "4c062a6", "be3563a" }
                );
        }

        [Fact]
        public void CanEnumerateUsingImplicitHeadAsSinceBoundary()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { ExcludeReachableFrom = "refs/heads/br2" },
                new[] { "4c062a6", "be3563a" }
                );
        }

        [Fact]
        public void CanEnumerateUsingTwoAbbreviatedShasAsBoundaries()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = "a4a7dce", ExcludeReachableFrom = "4a202b3" },
                new[] { "a4a7dce", "c47800c", "9fd738e" }
                );
        }

        [Fact]
        public void CanEnumerateCommitsFromTwoHeads()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = new[] { "refs/heads/br2", "refs/heads/master" } },
                new[]
                    {
                        "4c062a6", "a4a7dce", "be3563a", "c47800c",
                        "9fd738e", "4a202b3", "5b5b025", "8496071",
                    });
        }

        [Fact]
        public void CanEnumerateCommitsFromMixedStartingPoints()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = new object[] { repo.Branches["br2"],
                                                            "refs/heads/master",
                                                            new ObjectId("e90810b8df3e80c413d903f631643c716887138d") } },
                new[]
                    {
                        "4c062a6", "e90810b", "6dcf9bf", "a4a7dce",
                        "be3563a", "c47800c", "9fd738e", "4a202b3",
                        "5b5b025", "8496071",
                    });
        }

        [Fact]
        public void CanEnumerateCommitsUsingGlob()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = repo.Refs.FromGlob("refs/heads/*") },
                new[]
                   {
                       "4c062a6", "e90810b", "6dcf9bf", "a4a7dce", "be3563a", "c47800c", "9fd738e", "4a202b3", "41bc8c6", "5001298", "5b5b025", "8496071"
                   });
        }

        [Fact]
        public void CanHideCommitsUsingGlob()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = "refs/heads/packed-test", ExcludeReachableFrom = repo.Refs.FromGlob("*/packed") },
                new[]
                   {
                       "4a202b3", "5b5b025", "8496071"
                   });
        }

        [Fact]
        public void CanEnumerateCommitsFromAnAnnotatedTag()
        {
            CanEnumerateCommitsFromATag(t => t);
        }

        [Fact]
        public void CanEnumerateCommitsFromATagAnnotation()
        {
            CanEnumerateCommitsFromATag(t => t.Annotation);
        }

        private void CanEnumerateCommitsFromATag(Func<Tag, object> transformer)
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = transformer(repo.Tags["test"]) },
                new[] { "e90810b", "6dcf9bf", }
                );
        }

        [Fact]
        public void CanEnumerateAllCommits()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter
                    {
                        IncludeReachableFrom = repo.Refs.OrderBy(r => r.CanonicalName, StringComparer.Ordinal),
                    },
                new[]
                    {
                        "44d5d18", "bb65291", "532740a", "503a16f", "3dfd6fd",
                        "4409de1", "902c60b", "4c062a6", "e90810b", "6dcf9bf",
                        "a4a7dce", "be3563a", "c47800c", "9fd738e", "4a202b3",
                        "41bc8c6", "5001298", "5b5b025", "8496071",
                    });
        }

        [Fact]
        public void CanEnumerateCommitsFromATagWhichPointsToABlob()
        {
            AssertEnumerationOfCommits(
                repo => new CommitFilter { IncludeReachableFrom = repo.Tags["point_to_blob"] },
                new string[] { });
        }

        [Fact]
        public void CanEnumerateCommitsFromATagWhichPointsToATree()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                string headTreeSha = repo.Head.Tip.Tree.Sha;

                Tag tag = repo.ApplyTag("point_to_tree", headTreeSha);

                AssertEnumerationOfCommitsInRepo(repo,
                    r => new CommitFilter { IncludeReachableFrom = tag },
                    new string[] { });
            }
        }

        private void AssertEnumerationOfCommits(Func<IRepository, CommitFilter> filterBuilder, IEnumerable<string> abbrevIds)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                AssertEnumerationOfCommitsInRepo(repo, filterBuilder, abbrevIds);
            }
        }

        private static void AssertEnumerationOfCommitsInRepo(IRepository repo, Func<IRepository, CommitFilter> filterBuilder, IEnumerable<string> abbrevIds)
        {
            ICommitLog commits = repo.Commits.QueryBy(filterBuilder(repo));

            IEnumerable<string> commitShas = commits.Select(c => c.Id.ToString(7)).ToArray();

            Assert.Equal(abbrevIds, commitShas);
        }

        [Fact]
        public void CanLookupCommitGeneric()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commit = repo.Lookup<Commit>(sha);
                Assert.Equal("testing\n", commit.Message);
                Assert.Equal("testing", commit.MessageShort);
                Assert.Equal(sha, commit.Sha);
            }
        }

        [Fact]
        public void CanReadCommitData()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                GitObject obj = repo.Lookup(sha);
                Assert.NotNull(obj);
                Assert.Equal(typeof(Commit), obj.GetType());

                var commit = (Commit)obj;
                Assert.Equal("testing\n", commit.Message);
                Assert.Equal("testing", commit.MessageShort);
                Assert.Equal("UTF-8", commit.Encoding);
                Assert.Equal(sha, commit.Sha);

                Assert.NotNull(commit.Author);
                Assert.Equal("Scott Chacon", commit.Author.Name);
                Assert.Equal("schacon@gmail.com", commit.Author.Email);
                Assert.Equal(1273360386, commit.Author.When.ToSecondsSinceEpoch());

                Assert.NotNull(commit.Committer);
                Assert.Equal("Scott Chacon", commit.Committer.Name);
                Assert.Equal("schacon@gmail.com", commit.Committer.Email);
                Assert.Equal(1273360386, commit.Committer.When.ToSecondsSinceEpoch());

                Assert.Equal("181037049a54a1eb5fab404658a3a250b44335d7", commit.Tree.Sha);

                Assert.Equal(0, commit.Parents.Count());
            }
        }

        [Fact]
        public void CanReadCommitWithMultipleParents()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commit = repo.Lookup<Commit>("a4a7dce85cf63874e984719f4fdd239f5145052f");
                Assert.Equal(2, commit.Parents.Count());
            }
        }

        [Fact]
        public void CanDirectlyAccessABlobOfTheCommit()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commit = repo.Lookup<Commit>("4c062a6");

                var blob = commit["1/branch_file.txt"].Target as Blob;
                Assert.NotNull(blob);

                Assert.Equal("hi\n", blob.GetContentText());
            }
        }

        [Fact]
        public void CanDirectlyAccessATreeOfTheCommit()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commit = repo.Lookup<Commit>("4c062a6");

                var tree1 = commit["1"].Target as Tree;
                Assert.NotNull(tree1);
            }
        }

        [Fact]
        public void DirectlyAccessingAnUnknownTreeEntryOfTheCommitReturnsNull()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commit = repo.Lookup<Commit>("4c062a6");

                Assert.Null(commit["I-am-not-here"]);
            }
        }

        [Fact]
        public void CanCommitWithSignatureFromConfig()
        {
            string repoPath = InitNewRepository();
            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var options = new RepositoryOptions { GlobalConfigurationLocation = configPath };

            using (var repo = new Repository(repoPath, options))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                const string relativeFilepath = "new.txt";
                string filePath = Touch(repo.Info.WorkingDirectory, relativeFilepath, "null");
                repo.Stage(relativeFilepath);

                File.AppendAllText(filePath, "token\n");
                repo.Stage(relativeFilepath);

                Assert.Null(repo.Head[relativeFilepath]);

                Signature signature = repo.Config.BuildSignature(DateTimeOffset.Now);

                Commit commit = repo.Commit("Initial egotistic commit", signature, signature);

                AssertBlobContent(repo.Head[relativeFilepath], "nulltoken\n");
                AssertBlobContent(commit[relativeFilepath], "nulltoken\n");

                AssertCommitIdentitiesAre(commit, Constants.Identity);
            }
        }

        [Fact]
        public void CommitParentsAreMergeHeads()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard, "c47800");

                CreateAndStageANewFile(repo);

                Touch(repo.Info.Path, "MERGE_HEAD", "9fd738e8f7967c078dceed8190330fc8648ee56a\n");

                Assert.Equal(CurrentOperation.Merge, repo.Info.CurrentOperation);

                Commit newMergedCommit = repo.Commit("Merge commit", Constants.Signature, Constants.Signature);

                Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);

                Assert.Equal(2, newMergedCommit.Parents.Count());
                Assert.Equal(newMergedCommit.Parents.First().Sha, "c47800c7266a2be04c571c04d5a6614691ea99bd");
                Assert.Equal(newMergedCommit.Parents.Skip(1).First().Sha, "9fd738e8f7967c078dceed8190330fc8648ee56a");

                // Assert reflog entry is created
                var reflogEntry = repo.Refs.Log(repo.Refs.Head).First();
                Assert.Equal(repo.Head.Tip.Id, reflogEntry.To);
                Assert.NotNull(reflogEntry.Committer.Email);
                Assert.NotNull(reflogEntry.Committer.Name);
                Assert.Equal(string.Format("commit (merge): {0}", newMergedCommit.MessageShort), reflogEntry.Message);
            }
        }

        [Fact]
        public void CommitCleansUpMergeMetadata()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                const string relativeFilepath = "new.txt";
                Touch(repo.Info.WorkingDirectory, relativeFilepath, "this is a new file");
                repo.Stage(relativeFilepath);

                string mergeHeadPath = Touch(repo.Info.Path, "MERGE_HEAD", "abcdefabcdefabcdefabcdefabcdefabcdefabcd");
                string mergeMsgPath = Touch(repo.Info.Path, "MERGE_MSG", "This is a dummy merge.\n");
                string mergeModePath = Touch(repo.Info.Path, "MERGE_MODE", "no-ff");
                string origHeadPath = Touch(repo.Info.Path, "ORIG_HEAD", "beefbeefbeefbeefbeefbeefbeefbeefbeefbeef");

                Assert.True(File.Exists(mergeHeadPath));
                Assert.True(File.Exists(mergeMsgPath));
                Assert.True(File.Exists(mergeModePath));
                Assert.True(File.Exists(origHeadPath));

                var author = Constants.Signature;
                repo.Commit("Initial egotistic commit", author, author);

                Assert.False(File.Exists(mergeHeadPath));
                Assert.False(File.Exists(mergeMsgPath));
                Assert.False(File.Exists(mergeModePath));
                Assert.True(File.Exists(origHeadPath));
            }
        }

        [Fact]
        public void CanCommitALittleBit()
        {
            string repoPath = InitNewRepository();

            var identity = Constants.Identity;

            using (var repo = new Repository(repoPath, new RepositoryOptions { Identity = identity }))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                const string relativeFilepath = "new.txt";
                string filePath = Touch(repo.Info.WorkingDirectory, relativeFilepath, "null");
                repo.Stage(relativeFilepath);
                File.AppendAllText(filePath, "token\n");
                repo.Stage(relativeFilepath);

                Assert.Null(repo.Head[relativeFilepath]);

                var author = Constants.Signature;

                const string shortMessage = "Initial egotistic commit";
                const string commitMessage = shortMessage + "\n\nOnly the coolest commits from us";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Commit commit = repo.Commit(commitMessage, author, author);

                AssertBlobContent(repo.Head[relativeFilepath], "nulltoken\n");
                AssertBlobContent(commit[relativeFilepath], "nulltoken\n");

                Assert.Equal(0, commit.Parents.Count());
                Assert.False(repo.Info.IsHeadUnborn);

                // Assert a reflog entry is created on HEAD
                Assert.Equal(1, repo.Refs.Log("HEAD").Count());
                var reflogEntry = repo.Refs.Log("HEAD").First();

                Assert.Equal(identity.Name, reflogEntry.Committer.Name);
                Assert.Equal(identity.Email, reflogEntry.Committer.Email);

                var now = DateTimeOffset.Now;
                Assert.InRange(reflogEntry.Committer.When, before, now);

                Assert.Equal(commit.Id, reflogEntry.To);
                Assert.Equal(ObjectId.Zero, reflogEntry.From);
                Assert.Equal(string.Format("commit (initial): {0}", shortMessage), reflogEntry.Message);

                // Assert a reflog entry is created on HEAD target
                var targetCanonicalName = repo.Refs.Head.TargetIdentifier;
                Assert.Equal(1, repo.Refs.Log(targetCanonicalName).Count());
                Assert.Equal(commit.Id, repo.Refs.Log(targetCanonicalName).First().To);

                File.WriteAllText(filePath, "nulltoken commits!\n");
                repo.Stage(relativeFilepath);

                var author2 = new Signature(author.Name, author.Email, author.When.AddSeconds(5));
                Commit commit2 = repo.Commit("Are you trying to fork me?", author2, author2);

                AssertBlobContent(repo.Head[relativeFilepath], "nulltoken commits!\n");
                AssertBlobContent(commit2[relativeFilepath], "nulltoken commits!\n");

                Assert.Equal(1, commit2.Parents.Count());
                Assert.Equal(commit.Id, commit2.Parents.First().Id);

                // Assert the reflog is shifted
                Assert.Equal(2, repo.Refs.Log("HEAD").Count());
                Assert.Equal(reflogEntry.To, repo.Refs.Log("HEAD").First().From);

                Branch firstCommitBranch = repo.CreateBranch("davidfowl-rules", commit);
                repo.Checkout(firstCommitBranch);

                File.WriteAllText(filePath, "davidfowl commits!\n");

                var author3 = new Signature("David Fowler", "david.fowler@microsoft.com", author.When.AddSeconds(2));
                repo.Stage(relativeFilepath);

                Commit commit3 = repo.Commit("I'm going to branch you backwards in time!", author3, author3);

                AssertBlobContent(repo.Head[relativeFilepath], "davidfowl commits!\n");
                AssertBlobContent(commit3[relativeFilepath], "davidfowl commits!\n");

                Assert.Equal(1, commit3.Parents.Count());
                Assert.Equal(commit.Id, commit3.Parents.First().Id);

                AssertBlobContent(firstCommitBranch[relativeFilepath], "nulltoken\n");
            }
        }

        private static void AssertBlobContent(TreeEntry entry, string expectedContent)
        {
            Assert.Equal(TreeEntryTargetType.Blob, entry.TargetType);
            Assert.Equal(expectedContent, ((Blob)(entry.Target)).GetContentText());
        }

        private static void AddCommitToRepo(string path)
        {
            using (var repo = new Repository(path))
            {
                const string relativeFilepath = "test.txt";
                Touch(repo.Info.WorkingDirectory, relativeFilepath, "test\n");
                repo.Stage(relativeFilepath);

                var author = new Signature("nulltoken", "emeric.fermas@gmail.com", DateTimeOffset.Parse("Wed, Dec 14 2011 08:29:03 +0100"));
                repo.Commit("Initial commit", author, author);
            }
        }

        [Fact]
        public void CanGeneratePredictableObjectShas()
        {
            string repoPath = InitNewRepository();

            AddCommitToRepo(repoPath);

            using (var repo = new Repository(repoPath))
            {
                Commit commit = repo.Commits.Single();
                Assert.Equal("1fe3126578fc4eca68c193e4a3a0a14a0704624d", commit.Sha);
                Tree tree = commit.Tree;
                Assert.Equal("2b297e643c551e76cfa1f93810c50811382f9117", tree.Sha);

                GitObject blob = tree.Single().Target;
                Assert.IsAssignableFrom<Blob>(blob);
                Assert.Equal("9daeafb9864cf43055ae93beb0afd6c7d144bfa4", blob.Sha);
            }
        }

        [Fact]
        public void CanAmendARootCommit()
        {
            string repoPath = InitNewRepository();

            AddCommitToRepo(repoPath);

            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(1, repo.Head.Commits.Count());

                Commit originalCommit = repo.Head.Tip;
                Assert.Equal(0, originalCommit.Parents.Count());

                CreateAndStageANewFile(repo);

                Commit amendedCommit = repo.Commit("I'm rewriting the history!", Constants.Signature, Constants.Signature,
                    new CommitOptions { AmendPreviousCommit = true });

                Assert.Equal(1, repo.Head.Commits.Count());

                AssertCommitHasBeenAmended(repo, amendedCommit, originalCommit);
            }
        }

        [Fact]
        public void CanAmendACommitWithMoreThanOneParent()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                var mergedCommit = repo.Lookup<Commit>("be3563a");
                Assert.NotNull(mergedCommit);
                Assert.Equal(2, mergedCommit.Parents.Count());

                repo.Reset(ResetMode.Soft, mergedCommit.Sha);

                CreateAndStageANewFile(repo);
                const string commitMessage = "I'm rewriting the history!";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Commit amendedCommit = repo.Commit(commitMessage, Constants.Signature, Constants.Signature,
                    new CommitOptions { AmendPreviousCommit = true });

                AssertCommitHasBeenAmended(repo, amendedCommit, mergedCommit);

                AssertRefLogEntry(repo, "HEAD",
                                  string.Format("commit (amend): {0}", commitMessage),
                                  mergedCommit.Id,
                                  amendedCommit.Id,
                                  Constants.Identity, before);
            }
        }

        private static void CreateAndStageANewFile(IRepository repo)
        {
            string relativeFilepath = string.Format("new-file-{0}.txt", Path.GetRandomFileName());
            Touch(repo.Info.WorkingDirectory, relativeFilepath, "brand new content\n");
            repo.Stage(relativeFilepath);
        }

        private static void AssertCommitHasBeenAmended(IRepository repo, Commit amendedCommit, Commit originalCommit)
        {
            Commit headCommit = repo.Head.Tip;
            Assert.Equal(amendedCommit, headCommit);

            Assert.NotEqual(originalCommit.Sha, amendedCommit.Sha);
            Assert.Equal(originalCommit.Parents, amendedCommit.Parents);
        }

        [Fact]
        public void CanNotAmendAnEmptyRepository()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.Throws<UnbornBranchException>(() =>
                    repo.Commit("I can not amend anything !:(", Constants.Signature, Constants.Signature, new CommitOptions { AmendPreviousCommit = true }));
            }
        }

        [Fact]
        public void CanRetrieveChildrenOfASpecificCommit()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string parentSha = "5b5b025afb0b4c913b4c338a42934a3863bf3644";

                var filter = new CommitFilter
                                 {
                                     /* Revwalk from all the refs (git log --all) ... */
                                     IncludeReachableFrom = repo.Refs,

                                     /* ... and stop when the parent is reached */
                                     ExcludeReachableFrom = parentSha
                                 };

                var commits = repo.Commits.QueryBy(filter);

                var children = from c in commits
                            from p in c.Parents
                            let pId = p.Id
                            where pId.Sha == parentSha
                            select c;

                var expectedChildren = new[] { "c47800c7266a2be04c571c04d5a6614691ea99bd",
                                                "4a202b346bb0fb0db7eff3cffeb3c70babbd2045" };

                Assert.Equal(expectedChildren, children.Select(c => c.Id.Sha));
            }
        }

        [Fact]
        public void CanCorrectlyDistinguishAuthorFromCommitter()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var author = new Signature("Wilbert van Dolleweerd", "getit@xs4all.nl",
                                           Epoch.ToDateTimeOffset(1244187936, 120));
                var committer = new Signature("Henk Westhuis", "Henk_Westhuis@hotmail.com",
                                           Epoch.ToDateTimeOffset(1244286496, 120));

                Commit c = repo.Commit("I can haz an author and a committer!", author, committer);

                Assert.Equal(author, c.Author);
                Assert.Equal(committer, c.Committer);
            }
        }

        [Fact]
        public void CanCommitOnOrphanedBranch()
        {
            string newBranchName = "refs/heads/newBranch";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                // Set Head to point to branch other than master
                repo.Refs.UpdateTarget("HEAD", newBranchName);
                Assert.Equal(newBranchName, repo.Head.CanonicalName);

                const string relativeFilepath = "test.txt";
                Touch(repo.Info.WorkingDirectory, relativeFilepath, "test\n");
                repo.Stage(relativeFilepath);

                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);
                Assert.Equal(1, repo.Head.Commits.Count());
            }
        }

        [Fact]
        public void CanNotCommitAnEmptyCommit()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                Assert.Throws<EmptyCommitException>(() => repo.Commit("Empty commit!", Constants.Signature, Constants.Signature));
            }
        }

        [Fact]
        public void CanCommitAnEmptyCommitWhenForced()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                repo.Commit("Empty commit!", Constants.Signature, Constants.Signature,
                    new CommitOptions { AllowEmptyCommit = true });
            }
        }

        [Fact]
        public void CanNotAmendAnEmptyCommit()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                repo.Commit("Empty commit!", Constants.Signature, Constants.Signature,
                    new CommitOptions { AllowEmptyCommit = true });

                Assert.Throws<EmptyCommitException>(() => repo.Commit("Empty commit!", Constants.Signature, Constants.Signature,
                    new CommitOptions { AmendPreviousCommit = true }));
            }
        }

        [Fact]
        public void CanAmendAnEmptyCommitWhenForced()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                Commit emptyCommit = repo.Commit("Empty commit!", Constants.Signature, Constants.Signature,
                    new CommitOptions { AllowEmptyCommit = true });

                Commit amendedCommit = repo.Commit("I'm rewriting the history!", Constants.Signature, Constants.Signature,
                    new CommitOptions { AmendPreviousCommit = true, AllowEmptyCommit = true });
                AssertCommitHasBeenAmended(repo, amendedCommit, emptyCommit);
            }
        }

        [Fact]
        public void CanCommitAnEmptyCommitWhenMerging()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                Touch(repo.Info.Path, "MERGE_HEAD", "f705abffe7015f2beacf2abe7a36583ebee3487e\n");

                Assert.Equal(CurrentOperation.Merge, repo.Info.CurrentOperation);

                Commit newMergedCommit = repo.Commit("Merge commit", Constants.Signature, Constants.Signature);

                Assert.Equal(2, newMergedCommit.Parents.Count());
                Assert.Equal(newMergedCommit.Parents.First().Sha, "32eab9cb1f450b5fe7ab663462b77d7f4b703344");
                Assert.Equal(newMergedCommit.Parents.Skip(1).First().Sha, "f705abffe7015f2beacf2abe7a36583ebee3487e");
            }
        }

        [Fact]
        public void CanAmendAnEmptyMergeCommit()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                Touch(repo.Info.Path, "MERGE_HEAD", "f705abffe7015f2beacf2abe7a36583ebee3487e\n");
                Commit newMergedCommit = repo.Commit("Merge commit", Constants.Signature, Constants.Signature);

                Commit amendedCommit = repo.Commit("I'm rewriting the history!", Constants.Signature, Constants.Signature,
                    new CommitOptions { AmendPreviousCommit = true });
                AssertCommitHasBeenAmended(repo, amendedCommit, newMergedCommit);
            }
        }

        [Fact]
        public void CanNotAmendACommitInAWayThatWouldLeadTheNewCommitToBecomeEmpty()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Touch(repo.Info.WorkingDirectory, "test.txt", "test\n");
                repo.Stage("test.txt");

                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                Touch(repo.Info.WorkingDirectory, "new.txt", "content\n");
                repo.Stage("new.txt");

                repo.Commit("One commit", Constants.Signature, Constants.Signature);

                repo.Remove("new.txt");

                Assert.Throws<EmptyCommitException>(() => repo.Commit("Oops", Constants.Signature, Constants.Signature,
                    new CommitOptions { AmendPreviousCommit = true }));
            }
        }
    }
}
