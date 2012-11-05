using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class BranchFixture : BaseFixture
    {
        private readonly string[] expectedBranches = new[] { "br2", "master", "packed", "packed-test", "test", };

        [Theory]
        [InlineData("unit_test")]
        [InlineData("Ångström")]
        public void CanCreateBranch(string name)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch newBranch = repo.CreateBranch(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                Assert.NotNull(newBranch);
                Assert.Equal(name, newBranch.Name);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.NotNull(newBranch.Tip);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", newBranch.Tip.Sha);
                Assert.NotNull(repo.Branches.SingleOrDefault(p => p.Name == name));

                repo.Branches.Remove(newBranch.Name);
            }
        }

        [Fact]
        public void CanCreateBranchUsingAbbreviatedSha()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name, "be3563a");
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", newBranch.Tip.Sha);
            }
        }

        [Fact]
        public void CanCreateBranchFromImplicitHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name);
                Assert.NotNull(newBranch);
                Assert.Equal(name, newBranch.Name);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.False(newBranch.IsCurrentRepositoryHead);
                Assert.NotNull(newBranch.Tip);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", newBranch.Tip.Sha);
                Assert.NotNull(repo.Branches.SingleOrDefault(p => p.Name == name));
            }
        }

        [Fact]
        public void CanCreateBranchFromExplicitHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name, "HEAD");
                Assert.NotNull(newBranch);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", newBranch.Tip.Sha);
            }
        }

        [Fact]
        public void CanCreateBranchFromCommit()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var commit = repo.Lookup<Commit>("HEAD");
                Branch newBranch = repo.CreateBranch(name, commit);
                Assert.NotNull(newBranch);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", newBranch.Tip.Sha);
            }
        }

        [Fact]
        public void CanCreateBranchFromRevparseSpec()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "revparse_branch";
                var target = repo.Lookup<Commit>("master~2");
                Branch newBranch = repo.CreateBranch(name, target);
                Assert.NotNull(newBranch);
                Assert.Equal("9fd738e8f7967c078dceed8190330fc8648ee56a", newBranch.Tip.Sha);
            }
        }

        [Fact]
        public void CreatingABranchFromATagPeelsToTheCommit()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "i-peel-tag";
                Branch newBranch = repo.CreateBranch(name, "refs/tags/test");
                Assert.NotNull(newBranch);
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", newBranch.Tip.Sha);
            }
        }

        [Fact]
        public void CreatingABranchTriggersTheCreationOfADirectReference()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch newBranch = repo.CreateBranch("clone-of-master");
                Assert.False(newBranch.IsCurrentRepositoryHead);

                ObjectId commitId = repo.Head.Tip.Id;
                Assert.Equal(commitId, newBranch.Tip.Id);

                Reference reference = repo.Refs[newBranch.CanonicalName];
                Assert.NotNull(reference);
                Assert.IsType(typeof(DirectReference), reference);
            }
        }

        [Fact]
        public void CreatingABranchFromANonCommitObjectThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                const string name = "sorry-dude-i-do-not-do-blobs-nor-trees";
                Assert.Throws<LibGit2SharpException>(() => repo.CreateBranch(name, "refs/tags/point_to_blob"));
                Assert.Throws<LibGit2SharpException>(() => repo.CreateBranch(name, "53fc32d"));
                Assert.Throws<LibGit2SharpException>(() => repo.CreateBranch(name, "0266163"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownNamedTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Add("my_new_branch", "my_old_branch"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownShaTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Add("my_new_branch", Constants.UnknownSha));
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Add("my_new_branch", Constants.UnknownSha.Substring(0, 7)));
            }
        }

        [Fact]
        public void CreatingBranchWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Add(null, repo.Head.CanonicalName));
                Assert.Throws<ArgumentException>(() => repo.Branches.Add(string.Empty, repo.Head.CanonicalName));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Add("bad_branch", default(string)));
                Assert.Throws<ArgumentException>(() => repo.Branches.Add("bad_branch", string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.CreateBranch("bad_branch", default(Commit)));
            }
        }

        [Fact]
        public void CanListAllBranches()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedBranches, repo.Branches.Select(b => b.Name).ToArray());

                Assert.Equal(5, repo.Branches.Count());
            }
        }

        [Fact]
        public void CanListAllBranchesWhenGivenWorkingDir()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                var expectedWdBranches = new[] { "diff-test-cases", "i-do-numbers", "master", "track-local", "origin/HEAD", "origin/br2", "origin/master", "origin/packed-test", "origin/test" };

                Assert.Equal(expectedWdBranches, repo.Branches.Select(b => b.Name).ToArray());
            }
        }

        [Fact]
        public void CanListAllBranchesIncludingRemoteRefs()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var expectedBranchesIncludingRemoteRefs = new[]
                                                              {
                                                                  new { Name = "diff-test-cases", Sha = "e7039e6d0e7dd4d4c1e2e8e5aa5306b2776436ca", IsRemote = false },
                                                                  new { Name = "i-do-numbers", Sha = "7252fe2da2c4dd6d85231a150d0485ec46abaa7a", IsRemote = false },
                                                                  new { Name = "master", Sha = "32eab9cb1f450b5fe7ab663462b77d7f4b703344", IsRemote = false },
                                                                  new { Name = "track-local", Sha = "580c2111be43802dab11328176d94c391f1deae9", IsRemote = false },
                                                                  new { Name = "origin/HEAD", Sha = "580c2111be43802dab11328176d94c391f1deae9", IsRemote = true },
                                                                  new { Name = "origin/br2", Sha = "a4a7dce85cf63874e984719f4fdd239f5145052f", IsRemote = true },
                                                                  new { Name = "origin/master", Sha = "580c2111be43802dab11328176d94c391f1deae9", IsRemote = true },
                                                                  new { Name = "origin/packed-test", Sha = "4a202b346bb0fb0db7eff3cffeb3c70babbd2045", IsRemote = true },
                                                                  new { Name = "origin/test", Sha = "e90810b8df3e80c413d903f631643c716887138d", IsRemote = true },
                                                              };
                Assert.Equal(expectedBranchesIncludingRemoteRefs, repo.Branches.Select(b => new { b.Name, b.Tip.Sha, b.IsRemote }).ToArray());
            }
        }

        [Fact]
        public void CanResolveRemote()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                Assert.Equal(repo.Remotes["origin"], master.Remote);
            }
        }

        [Fact]
        public void RemoteForNonTrackingBranchIsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch test = repo.Branches["i-do-numbers"];
                Assert.Null(test.Remote);
            }
        }

        [Fact]
        public void QueryRemoteForLocalTrackingBranch()
        {
            // There is not a Remote to resolve for a local tracking branch.
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch trackLocal = repo.Branches["track-local"];
                Assert.Null(trackLocal.Remote);
            }
        }

        [Fact]
        public void CanLookupABranchByItsCanonicalName()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch branch = repo.Branches["refs/heads/br2"];
                Assert.NotNull(branch);
                Assert.Equal("br2", branch.Name);

                Branch branch2 = repo.Branches["refs/heads/br2"];
                Assert.NotNull(branch2);
                Assert.Equal("br2", branch2.Name);

                Assert.Equal(branch, branch2);
                Assert.True((branch2 == branch));
            }
        }

        [Fact]
        public void CanLookupLocalBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                Assert.NotNull(master);
                Assert.False(master.IsRemote);
                Assert.Equal("master", master.Name);
                Assert.Equal("refs/heads/master", master.CanonicalName);
                Assert.True(master.IsCurrentRepositoryHead);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", master.Tip.Sha);
            }
        }

        [Fact]
        public void CanLookupABranchWhichNameIsMadeOfNon7BitsAsciiCharacters()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "Ångström";
                Branch newBranch = repo.CreateBranch(name, "be3563a");
                Assert.NotNull(newBranch);

                Branch retrieved = repo.Branches["Ångström"];
                Assert.NotNull(retrieved);
                Assert.Equal(newBranch.Tip, retrieved.Tip);
            }
        }

        [Fact]
        public void LookingOutABranchByNameWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch branch;
                Assert.Throws<ArgumentNullException>(() => branch = repo.Branches[null]);
                Assert.Throws<ArgumentException>(() => branch = repo.Branches[""]);
            }
        }

        public void CanGetInformationFromUnbornBranch()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.DirectoryPath, true))
            {
                var head = repo.Head;

                Assert.Equal("refs/heads/master", head.CanonicalName);
                Assert.Equal(0, head.Commits.Count());
                Assert.True(head.IsCurrentRepositoryHead);
                Assert.False(head.IsRemote);
                Assert.Equal("master", head.Name);
                Assert.Null(head.Tip);
                Assert.Null(head["huh?"]);

                Assert.Null(head.AheadBy);
                Assert.Null(head.BehindBy);
                Assert.False(head.IsTracking);
                Assert.Null(head.TrackedBranch);
            }
        }

        [Fact]
        public void CanGetTrackingInformationFromBranchSharingNoHistoryWithItsTrackedBranch()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                repo.Refs.UpdateTarget("refs/remotes/origin/master", "origin/test");

                Assert.True(master.IsTracking);
                Assert.Null(master.AheadBy);
                Assert.Null(master.BehindBy);
                Assert.NotNull(master.TrackedBranch);
            }
        }

        [Fact]
        public void TrackingInformationIsEmptyForNonTrackingBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch branch = repo.Branches["test"];
                Assert.False(branch.IsTracking);
                Assert.Null(branch.TrackedBranch);
                Assert.Null(branch.AheadBy);
                Assert.Null(branch.BehindBy);
            }
        }

        [Fact]
        public void CanGetTrackingInformationForTrackingBranch()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsTracking);
                Assert.Equal(repo.Branches["refs/remotes/origin/master"], master.TrackedBranch);
                Assert.Equal(2, master.AheadBy);
                Assert.Equal(2, master.BehindBy);
            }
        }

        [Fact]
        public void CanGetTrackingInformationForLocalTrackingBranch()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var branch = repo.Branches["track-local"];
                Assert.True(branch.IsTracking);
                Assert.Equal(repo.Branches["master"], branch.TrackedBranch);
                Assert.Equal(2, branch.AheadBy);
                Assert.Equal(2, branch.BehindBy);
            }
        }

        [Fact]
        public void MovingARemoteTrackingBranchThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch master = repo.Branches["refs/remotes/origin/master"];
                Assert.True(master.IsRemote);

                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Move(master, "new_name", true));
            }
        }

        [Fact]
        public void CanWalkCommitsFromAnotherBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch master = repo.Branches["test"];
                Assert.Equal(2, master.Commits.Count());
            }
        }

        [Fact]
        public void CanWalkCommitsFromBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                Assert.Equal(7, master.Commits.Count());
            }
        }

        private void AssertRemoval(string branchName, bool isRemote, bool shouldPreviouslyAssertExistence)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                if (shouldPreviouslyAssertExistence)
                {
                    Assert.NotNull(repo.Branches[branchName]);
                }

                repo.Branches.Remove(branchName, isRemote);
                Branch branch = repo.Branches[branchName];
                Assert.Null(branch);
            }
        }

        [Theory]
        [InlineData("i-do-numbers", false)]
        [InlineData("origin/br2", true)]
        public void CanRemoveAnExistingNamedBranch(string branchName, bool isRemote)
        {
            AssertRemoval(branchName, isRemote, true);
        }

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("origin/br2")]
        public void CanRemoveAnExistingBranch(string branchName)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch curBranch = repo.Branches[branchName];

                repo.Branches.Remove(curBranch);
                Branch branch = repo.Branches[branchName];
                Assert.Null(branch);
            }
        }

        [Theory]
        [InlineData("I-donot-exist", false)]
        [InlineData("me/neither", true)]
        public void CanRemoveANonExistingBranch(string branchName, bool isRemote)
        {
            AssertRemoval(branchName, isRemote, false);
        }

        [Fact]
        public void RemovingABranchWhichIsTheCurrentHeadThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Remove(repo.Head.Name));
            }
        }

        [Fact]
        public void RemovingABranchWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Remove(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Remove(null));
            }
        }

        [Fact]
        public void OnlyOneBranchIsTheHead()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch head = null;

                foreach (Branch branch in repo.Branches)
                {
                    bool isHead = branch.IsCurrentRepositoryHead;

                    if (!isHead)
                    {
                        continue;
                    }

                    if (head == null)
                    {
                        head = branch;
                        continue;
                    }

                    Assert.True(false, string.Format("Both '{0}' and '{1}' appear to be Head.", head.CanonicalName, branch.CanonicalName));
                }

                Assert.NotNull(head);
            }
        }

        [Fact]
        public void TwoBranchesPointingAtTheSameCommitAreNotBothCurrent()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["refs/heads/master"];

                Branch newBranch = repo.Branches.Add("clone-of-master", master.Tip.Sha);
                Assert.False(newBranch.IsCurrentRepositoryHead);
            }
        }

        [Fact]
        public void CanMoveABranch()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Null(repo.Branches["br3"]);

                Branch newBranch = repo.Branches.Move("br2", "br3");
                Assert.Equal("br3", newBranch.Name);

                Assert.Null(repo.Branches["br2"]);
                Assert.NotNull(repo.Branches["br3"]);
            }
        }

        [Fact]
        public void BlindlyMovingABranchOverAnExistingOneThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Move("br2", "test"));
            }
        }

        [Fact]
        public void CanMoveABranchWhileOverwritingAnExistingOne()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch test = repo.Branches["test"];
                Assert.NotNull(test);

                Branch br2 = repo.Branches["br2"];
                Assert.NotNull(br2);

                Branch newBranch = repo.Branches.Move("br2", "test", true);
                Assert.Equal("test", newBranch.Name);

                Assert.Null(repo.Branches["br2"]);

                Branch newTest = repo.Branches["test"];
                Assert.NotNull(newTest);
                Assert.Equal(newBranch, newTest);

                Assert.Equal(br2.Tip, newTest.Tip);
            }
        }
    }
}
