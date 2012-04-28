using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class CommitFixture : BaseFixture
    {
        private const string sha = "8496071c1b46c854b31185ea97743be6a8774479";
        private readonly List<string> expectedShas = new List<string> { "a4a7d", "c4780", "9fd73", "4a202", "5b5b0", "84960" };

        [Fact]
        public void CanCountCommits()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Commits.Count().ShouldEqual(7);
            }
        }

        [Fact]
        public void CanCorrectlyCountCommitsWhenSwitchingToAnotherBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Checkout("test");
                repo.Commits.Count().ShouldEqual(2);
                repo.Commits.First().Id.Sha.ShouldEqual("e90810b8df3e80c413d903f631643c716887138d");

                repo.Checkout("master");
                repo.Commits.Count().ShouldEqual(7);
                repo.Commits.First().Id.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
            }
        }

        [Fact]
        public void CanEnumerateCommits()
        {
            int count = 0;
            using (var repo = new Repository(BareTestRepoPath))
            {
                foreach (Commit commit in repo.Commits)
                {
                    commit.ShouldNotBeNull();
                    count++;
                }
            }
            count.ShouldEqual(7);
        }

        [Fact]
        public void CanEnumerateCommitsInDetachedHeadState()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                ObjectId parentOfHead = repo.Head.Tip.Parents.First().Id;

                repo.Refs.Create("HEAD", parentOfHead.Sha, true);
                Assert.Equal(true, repo.Info.IsHeadDetached);

                repo.Commits.Count().ShouldEqual(6);
            }
        }

        [Fact]
        public void DefaultOrderingWhenEnumeratingCommitsIsTimeBased()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Commits.SortedBy.ShouldEqual(GitSortOptions.Time);
            }
        }

        [Fact]
        public void CanEnumerateCommitsFromSha()
        {
            int count = 0;
            using (var repo = new Repository(BareTestRepoPath))
            {
                foreach (Commit commit in repo.Commits.QueryBy(new Filter { Since = "a4a7dce85cf63874e984719f4fdd239f5145052f" }))
                {
                    commit.ShouldNotBeNull();
                    count++;
                }
            }
            count.ShouldEqual(6);
        }

        [Fact]
        public void QueryingTheCommitHistoryWithUnknownShaOrInvalidEntryPointThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Commits.QueryBy(new Filter { Since = Constants.UnknownSha }).Count());
                Assert.Throws<LibGit2Exception>(() => repo.Commits.QueryBy(new Filter { Since = "refs/heads/deadbeef" }).Count());
                Assert.Throws<ArgumentNullException>(() => repo.Commits.QueryBy(new Filter { Since = null }).Count());
            }
        }

        [Fact]
        public void QueryingTheCommitHistoryFromACorruptedReferenceThrows()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                CreateCorruptedDeadBeefHead(repo.Info.Path);

                Assert.Throws<LibGit2Exception>(() => repo.Commits.QueryBy(new Filter { Since = repo.Branches["deadbeef"] }).Count());
                Assert.Throws<LibGit2Exception>(() => repo.Commits.QueryBy(new Filter { Since = repo.Refs["refs/heads/deadbeef"] }).Count());
            }
        }

        [Fact]
        public void QueryingTheCommitHistoryWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Commits.QueryBy(new Filter { Since = string.Empty }));
                Assert.Throws<ArgumentNullException>(() => repo.Commits.QueryBy(new Filter { Since = null }));
                Assert.Throws<ArgumentNullException>(() => repo.Commits.QueryBy(null));
            }
        }

        [Fact]
        public void CanEnumerateCommitsWithReverseTimeSorting()
        {
            var reversedShas = new List<string>(expectedShas);
            reversedShas.Reverse();

            int count = 0;
            using (var repo = new Repository(BareTestRepoPath))
            {
                foreach (Commit commit in repo.Commits.QueryBy(new Filter { Since = "a4a7dce85cf63874e984719f4fdd239f5145052f", SortBy = GitSortOptions.Time | GitSortOptions.Reverse }))
                {
                    commit.ShouldNotBeNull();
                    commit.Sha.StartsWith(reversedShas[count]).ShouldBeTrue();
                    count++;
                }
            }
            count.ShouldEqual(6);
        }

        [Fact]
        public void CanEnumerateCommitsWithReverseTopoSorting()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                List<Commit> commits = repo.Commits.QueryBy(new Filter { Since = "a4a7dce85cf63874e984719f4fdd239f5145052f", SortBy = GitSortOptions.Time | GitSortOptions.Reverse }).ToList();
                foreach (Commit commit in commits)
                {
                    commit.ShouldNotBeNull();
                    foreach (Commit p in commit.Parents)
                    {
                        Commit parent = commits.Single(x => x.Id == p.Id);
                        Assert.True(commits.IndexOf(commit) > commits.IndexOf(parent));
                    }
                }
            }
        }

        [Fact]
        public void CanGetParentsCount()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                repo.Commits.First().ParentsCount.ShouldEqual(1);
            }
        }

        [Fact]
        public void CanEnumerateCommitsWithTimeSorting()
        {
            int count = 0;
            using (var repo = new Repository(BareTestRepoPath))
            {
                foreach (Commit commit in repo.Commits.QueryBy(new Filter { Since = "a4a7dce85cf63874e984719f4fdd239f5145052f", SortBy = GitSortOptions.Time }))
                {
                    commit.ShouldNotBeNull();
                    commit.Sha.StartsWith(expectedShas[count]).ShouldBeTrue();
                    count++;
                }
            }
            count.ShouldEqual(6);
        }

        [Fact]
        public void CanEnumerateCommitsWithTopoSorting()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                List<Commit> commits = repo.Commits.QueryBy(new Filter { Since = "a4a7dce85cf63874e984719f4fdd239f5145052f", SortBy = GitSortOptions.Topological }).ToList();
                foreach (Commit commit in commits)
                {
                    commit.ShouldNotBeNull();
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
                repo => new Filter { Since = repo.Head },
                new[]
                    {
                        "4c062a6", "be3563a", "c47800c", "9fd738e",
                        "4a202b3", "5b5b025", "8496071",
                    });
        }

        [Fact]
        public void CanEnumerateFromDetachedHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repoClone = new Repository(path.RepositoryPath))
            {
                string headSha = repoClone.Head.Tip.Sha;
                repoClone.Checkout(headSha);

                AssertEnumerationOfCommitsInRepo(repoClone,
                    repo => new Filter { Since = repo.Head },
                    new[]
                        {
                            "4c062a6", "be3563a", "c47800c", "9fd738e",
                            "4a202b3", "5b5b025", "8496071",
                        });
            }
        }

        [Fact]
        public void CanEnumerateUsingTwoHeadsAsBoundaries()
        {
            AssertEnumerationOfCommits(
                repo => new Filter { Since = "HEAD", Until = "refs/heads/br2" },
                new[] { "4c062a6", "be3563a" }
                );
        }

        [Fact]
        public void CanEnumerateUsingImplicitHeadAsSinceBoundary()
        {
            AssertEnumerationOfCommits(
                repo => new Filter { Until = "refs/heads/br2" },
                new[] { "4c062a6", "be3563a" }
                );
        }

        [Fact]
        public void CanEnumerateUsingTwoAbbreviatedShasAsBoundaries()
        {
            AssertEnumerationOfCommits(
                repo => new Filter { Since = "a4a7dce", Until = "4a202b3" },
                new[] { "a4a7dce", "c47800c", "9fd738e" }
                );
        }

        [Fact]
        public void CanEnumerateCommitsFromTwoHeads()
        {
            AssertEnumerationOfCommits(
                repo => new Filter { Since = new[] { "refs/heads/br2", "refs/heads/master" } },
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
                repo => new Filter { Since = new object[] { repo.Branches["br2"], "refs/heads/master", new ObjectId("e90810b") } },
                new[]
                    {
                        "4c062a6", "e90810b", "6dcf9bf", "a4a7dce",
                        "be3563a", "c47800c", "9fd738e", "4a202b3",
                        "5b5b025", "8496071",
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

        private static void CanEnumerateCommitsFromATag(Func<Tag, object> transformer)
        {
            AssertEnumerationOfCommits(
                repo => new Filter { Since = transformer(repo.Tags["test"]) },
                new[] { "e90810b", "6dcf9bf", }
                );
        }

        [Fact]
        public void CanEnumerateAllCommits()
        {
            AssertEnumerationOfCommits(
                repo => new Filter { Since = repo.Refs },
                new[]
                    {
                        "44d5d18", "bb65291", "532740a", "503a16f", "3dfd6fd", 
                        "4409de1", "902c60b", "4c062a6", "e90810b", "6dcf9bf", 
                        "a4a7dce", "be3563a", "c47800c", "9fd738e", "4a202b3", 
                        "41bc8c6", "5001298", "5b5b025", "8496071",
                    });
        }

        [Fact]
        public void CanEnumerateCommitsFromATagWhichDoesNotPointAtACommit()
        {
            AssertEnumerationOfCommits(
                repo => new Filter { Since = repo.Tags["point_to_blob"] },
                new string[] { });
        }

        private static void AssertEnumerationOfCommits(Func<Repository, Filter> filterBuilder, IEnumerable<string> abbrevIds)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                AssertEnumerationOfCommitsInRepo(repo, filterBuilder, abbrevIds);
            }
        }

        private static void AssertEnumerationOfCommitsInRepo(Repository repo, Func<Repository, Filter> filterBuilder, IEnumerable<string> abbrevIds)
        {
            ICommitCollection commits = repo.Commits.QueryBy(filterBuilder(repo));

            IEnumerable<string> commitShas = commits.Select(c => c.Id.ToString(7)).ToArray();

            Assert.Equal(abbrevIds, commitShas);
        }

        [Fact]
        public void CanLookupCommitGeneric()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var commit = repo.Lookup<Commit>(sha);
                commit.Message.ShouldEqual("testing\n");
                commit.MessageShort.ShouldEqual("testing");
                commit.Sha.ShouldEqual(sha);
            }
        }

        [Fact]
        public void CanReadCommitData()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                GitObject obj = repo.Lookup(sha);
                obj.ShouldNotBeNull();
                obj.GetType().ShouldEqual(typeof(Commit));

                var commit = (Commit)obj;
                commit.Message.ShouldEqual("testing\n");
                commit.MessageShort.ShouldEqual("testing");
                commit.Encoding.ShouldEqual("UTF-8");
                commit.Sha.ShouldEqual(sha);

                commit.Author.ShouldNotBeNull();
                commit.Author.Name.ShouldEqual("Scott Chacon");
                commit.Author.Email.ShouldEqual("schacon@gmail.com");
                commit.Author.When.ToSecondsSinceEpoch().ShouldEqual(1273360386);

                commit.Committer.ShouldNotBeNull();
                commit.Committer.Name.ShouldEqual("Scott Chacon");
                commit.Committer.Email.ShouldEqual("schacon@gmail.com");
                commit.Committer.When.ToSecondsSinceEpoch().ShouldEqual(1273360386);

                commit.Tree.Sha.ShouldEqual("181037049a54a1eb5fab404658a3a250b44335d7");

                commit.ParentsCount.ShouldEqual(0);
            }
        }

        [Fact]
        public void CanReadCommitWithMultipleParents()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var commit = repo.Lookup<Commit>("a4a7dce85cf63874e984719f4fdd239f5145052f");
                commit.Parents.Count().ShouldEqual(2);
                commit.ParentsCount.ShouldEqual(2);
            }
        }

        [Fact]
        public void CanDirectlyAccessABlobOfTheCommit()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var commit = repo.Lookup<Commit>("4c062a6");

                var blob = commit["1/branch_file.txt"].Target as Blob;
                blob.ShouldNotBeNull();

                blob.ContentAsUtf8().ShouldEqual("hi\n");
            }
        }

        [Fact]
        public void CanDirectlyAccessATreeOfTheCommit()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var commit = repo.Lookup<Commit>("4c062a6");

                var tree1 = commit["1"].Target as Tree;
                tree1.ShouldNotBeNull();
            }
        }

        [Fact]
        public void DirectlyAccessingAnUnknownTreeEntryOfTheCommitReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var commit = repo.Lookup<Commit>("4c062a6");

                commit["I-am-not-here"].ShouldBeNull();
            }
        }

        [SkippableFact]
        public void CanCommitWithSignatureFromConfig()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath)) 
            {
                string dir = repo.Info.Path;
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();

                InconclusiveIf(() => !repo.Config.HasGlobalConfig, "No Git global configuration available");

                const string relativeFilepath = "new.txt";
                string filePath = Path.Combine(repo.Info.WorkingDirectory, relativeFilepath);

                File.WriteAllText(filePath, "null");
                repo.Index.Stage(relativeFilepath);
                File.AppendAllText(filePath, "token\n");
                repo.Index.Stage(relativeFilepath);

                repo.Head[relativeFilepath].ShouldBeNull();

                Commit commit = repo.Commit("Initial egotistic commit");

                AssertBlobContent(repo.Head[relativeFilepath], "nulltoken\n");
                AssertBlobContent(commit[relativeFilepath], "nulltoken\n");

                var name = repo.Config.Get<string>("user.name", null);
                var email = repo.Config.Get<string>("user.email", null);
                Assert.Equal(commit.Author.Name, name);
                Assert.Equal(commit.Author.Email, email);
                Assert.Equal(commit.Committer.Name, name);
                Assert.Equal(commit.Committer.Email, email);
            }
        }

        [Fact]
        public void CanCommitALittleBit()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                string dir = repo.Info.Path;
                Path.IsPathRooted(dir).ShouldBeTrue();
                Directory.Exists(dir).ShouldBeTrue();

                const string relativeFilepath = "new.txt";
                string filePath = Path.Combine(repo.Info.WorkingDirectory, relativeFilepath);

                File.WriteAllText(filePath, "null");
                repo.Index.Stage(relativeFilepath);
                File.AppendAllText(filePath, "token\n");
                repo.Index.Stage(relativeFilepath);

                repo.Head[relativeFilepath].ShouldBeNull();

                var author = DummySignature;
                Commit commit = repo.Commit("Initial egotistic commit", author, author);

                AssertBlobContent(repo.Head[relativeFilepath], "nulltoken\n");
                AssertBlobContent(commit[relativeFilepath], "nulltoken\n");

                commit.ParentsCount.ShouldEqual(0);
                repo.Info.IsEmpty.ShouldBeFalse();

                File.WriteAllText(filePath, "nulltoken commits!\n");
                repo.Index.Stage(relativeFilepath);

                var author2 = new Signature(author.Name, author.Email, author.When.AddSeconds(5));
                Commit commit2 = repo.Commit("Are you trying to fork me?", author2, author2);

                AssertBlobContent(repo.Head[relativeFilepath], "nulltoken commits!\n");
                AssertBlobContent(commit2[relativeFilepath], "nulltoken commits!\n");

                commit2.ParentsCount.ShouldEqual(1);
                commit2.Parents.First().Id.ShouldEqual(commit.Id);

                Branch firstCommitBranch = repo.CreateBranch("davidfowl-rules", commit);
                repo.Checkout(firstCommitBranch);

                File.WriteAllText(filePath, "davidfowl commits!\n");

                var author3 = new Signature("David Fowler", "david.fowler@microsoft.com", author.When.AddSeconds(2));
                repo.Index.Stage(relativeFilepath);

                Commit commit3 = repo.Commit("I'm going to branch you backwards in time!", author3, author3);

                AssertBlobContent(repo.Head[relativeFilepath], "davidfowl commits!\n");
                AssertBlobContent(commit3[relativeFilepath], "davidfowl commits!\n");

                commit3.ParentsCount.ShouldEqual(1);
                commit3.Parents.First().Id.ShouldEqual(commit.Id);

                AssertBlobContent(firstCommitBranch[relativeFilepath], "nulltoken\n");
            }
        }

        private static void AssertBlobContent(TreeEntry entry, string expectedContent)
        {
            entry.Type.ShouldEqual(GitObjectType.Blob);
            ((Blob)(entry.Target)).ContentAsUtf8().ShouldEqual(expectedContent);
        }

        private static void CommitToANewRepository(string path)
        {
            using (Repository repo = Repository.Init(path))
            {
                const string relativeFilepath = "test.txt";
                string filePath = Path.Combine(repo.Info.WorkingDirectory, relativeFilepath);

                File.WriteAllText(filePath, "test\n");
                repo.Index.Stage(relativeFilepath);

                var author = new Signature("nulltoken", "emeric.fermas@gmail.com", DateTimeOffset.Parse("Wed, Dec 14 2011 08:29:03 +0100"));
                repo.Commit("Initial commit", author, author);
            }
        }

        [Fact]
        public void CanGeneratePredictableObjectShas()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            CommitToANewRepository(scd.DirectoryPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                Commit commit = repo.Commits.Single();
                commit.Sha.ShouldEqual("1fe3126578fc4eca68c193e4a3a0a14a0704624d");
                Tree tree = commit.Tree;
                tree.Sha.ShouldEqual("2b297e643c551e76cfa1f93810c50811382f9117");

                Blob blob = tree.Blobs.Single();
                blob.Sha.ShouldEqual("9daeafb9864cf43055ae93beb0afd6c7d144bfa4");
            }
        }

        [Fact]
        public void CanAmendARootCommit()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            CommitToANewRepository(scd.DirectoryPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                repo.Head.Commits.Count().ShouldEqual(1);

                Commit originalCommit = repo.Head.Tip;
                originalCommit.ParentsCount.ShouldEqual(0);

                CreateAndStageANewFile(repo);

                Commit amendedCommit = repo.Commit("I'm rewriting the history!", DummySignature, DummySignature, true);

                repo.Head.Commits.Count().ShouldEqual(1);

                AssertCommitHasBeenAmended(repo, amendedCommit, originalCommit);
            }
        }

        [Fact]
        public void CanAmendACommitWithMoreThanOneParent()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var mergedCommit = repo.Lookup<Commit>("be3563a");
                mergedCommit.ShouldNotBeNull();
                mergedCommit.ParentsCount.ShouldEqual(2);

                repo.Reset(ResetOptions.Soft, mergedCommit.Sha);

                CreateAndStageANewFile(repo);

                Commit amendedCommit = repo.Commit("I'm rewriting the history!", DummySignature, DummySignature, true);

                AssertCommitHasBeenAmended(repo, amendedCommit, mergedCommit);
            }
        }

        private static void CreateAndStageANewFile(Repository repo)
        {
            string relativeFilepath = string.Format("new-file-{0}.txt", Guid.NewGuid());
            string filePath = Path.Combine(repo.Info.WorkingDirectory, relativeFilepath);

            File.WriteAllText(filePath, "brand new content\n");
            repo.Index.Stage(relativeFilepath);
        }

        private void AssertCommitHasBeenAmended(Repository repo, Commit amendedCommit, Commit originalCommit)
        {
            Commit headCommit = repo.Head.Tip;
            headCommit.ShouldEqual(amendedCommit);

            amendedCommit.Sha.ShouldNotEqual(originalCommit.Sha);
            Assert.Equal(originalCommit.Parents, amendedCommit.Parents);
        }

        [Fact]
        public void CanNotAmendAnEmptyRepository()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (Repository repo = Repository.Init(scd.DirectoryPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Commit("I can not amend anything !:(", DummySignature, DummySignature, true));
            }
        }

        [Fact]
        public void CanRetrieveChildrenOfASpecificCommit()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string parentSha = "5b5b025afb0b4c913b4c338a42934a3863bf3644";

                var filter = new Filter
                                 {
                                     /* Revwalk from all the refs (git log --all) ... */
                                     Since = repo.Refs,

                                     /* ... and stop when the parent is reached */
                                     Until = parentSha
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
    }
}
