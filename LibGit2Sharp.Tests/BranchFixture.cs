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

        [Fact]
        public void CanCreateBranch()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var newBranch = repo.CreateBranch(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                newBranch.ShouldNotBeNull();
                newBranch.Name.ShouldEqual(name);
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.Tip.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                repo.Branches.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();

                repo.Branches.Delete(newBranch.Name);
            }
        }

        [Fact]
        public void CanCreateBranchUsingAbbreviatedSha()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var newBranch = repo.CreateBranch(name, "be3563a");
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.Tip.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
            }
        }

        [Fact]
        public void CanCreateBranchFromImplicitHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var newBranch = repo.CreateBranch(name);
                newBranch.ShouldNotBeNull();
                newBranch.Name.ShouldEqual(name);
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();
                newBranch.Tip.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                repo.Branches.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();
            }
        }

        [Fact]
        public void CanCreateBranchFromExplicitHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var newBranch = repo.CreateBranch(name, "HEAD");
                newBranch.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
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
                var newBranch = repo.CreateBranch(name, commit);
                newBranch.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
            }
        }

        [Fact]
        public void CreatingABranchFromATagPeelsToTheCommit()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "i-peel-tag";
                var newBranch = repo.CreateBranch(name, "refs/tags/test");
                newBranch.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("e90810b8df3e80c413d903f631643c716887138d");
            }
        }

        [Fact]
        public void CreatingABranchTriggersTheCreationOfADirectReference()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newBranch = repo.CreateBranch("clone-of-master");
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();

                ObjectId commitId = repo.Head.Tip.Id;
                newBranch.Tip.Id.ShouldEqual(commitId);

                Reference reference = repo.Refs[newBranch.CanonicalName];
                reference.ShouldNotBeNull();
                Assert.IsType(typeof(DirectReference), reference);
            }
        }

        [Fact]
        public void CreatingABranchFromANonCommitObjectThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                const string name = "sorry-dude-i-do-not-do-blobs-nor-trees";
                Assert.Throws<LibGit2Exception>(() => repo.CreateBranch(name, "refs/tags/point_to_blob"));
                Assert.Throws<LibGit2Exception>(() => repo.CreateBranch(name, "53fc32d"));
                Assert.Throws<LibGit2Exception>(() => repo.CreateBranch(name, "0266163"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownNamedTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("my_new_branch", "my_old_branch"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownShaTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("my_new_branch", Constants.UnknownSha));
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("my_new_branch", Constants.UnknownSha.Substring(0, 7)));
            }
        }

        [Fact]
        public void CreatingABranchPointingAtANonCanonicalReferenceThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("nocanonicaltarget", "br2"));
            }
        }

        [Fact]
        public void CreatingBranchWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create(null, repo.Head.CanonicalName));
                Assert.Throws<ArgumentException>(() => repo.Branches.Create(string.Empty, repo.Head.CanonicalName));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create("bad_branch", default(string)));
                Assert.Throws<ArgumentException>(() => repo.Branches.Create("bad_branch", string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.CreateBranch("bad_branch", default(Commit)));
            }
        }

        [Fact]
        public void CanListAllBranches()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedBranches, repo.Branches.Select(b => b.Name).ToArray());

                repo.Branches.Count().ShouldEqual(5);
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
        public void CanLookupABranchByItsCanonicalName()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var branch = repo.Branches["refs/heads/br2"];
                branch.ShouldNotBeNull();
                branch.Name.ShouldEqual("br2");

                var branch2 = repo.Branches["refs/heads/br2"];
                branch2.ShouldNotBeNull();
                branch2.Name.ShouldEqual("br2");

                branch2.ShouldEqual(branch);
                (branch2 == branch).ShouldBeTrue();
            }
        }

        [Fact]
        public void CanLookupLocalBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var master = repo.Branches["master"];
                master.ShouldNotBeNull();
                master.IsRemote.ShouldBeFalse();
                master.Name.ShouldEqual("master");
                master.CanonicalName.ShouldEqual("refs/heads/master");
                master.IsCurrentRepositoryHead.ShouldBeTrue();
                master.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
            }
        }

        [Fact]
        public void LookingOutABranchByNameWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                IBranch branch;
                Assert.Throws<ArgumentNullException>(() => branch = repo.Branches[null]);
                Assert.Throws<ArgumentException>(() => branch = repo.Branches[""]);
            }
        }

        [Fact]
        public void TrackingInformationIsEmptyForNonTrackingBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var branch = repo.Branches["test"];
                branch.IsTracking.ShouldBeFalse();
                branch.TrackedBranch.ShouldBeNull();
                branch.AheadBy.ShouldEqual(0);
                branch.BehindBy.ShouldEqual(0);
            }
        }

        [Fact]
        public void CanGetTrackingInformationForTrackingBranch()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var master = repo.Branches["master"];
                master.IsTracking.ShouldBeTrue();
                master.TrackedBranch.ShouldEqual(repo.Branches["refs/remotes/origin/master"]);
                master.AheadBy.ShouldEqual(2);
                master.BehindBy.ShouldEqual(2);
            }
        }

        [Fact]
        public void CanGetTrackingInformationForLocalTrackingBranch()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var branch = repo.Branches["track-local"];
                branch.IsTracking.ShouldBeTrue();
                branch.TrackedBranch.ShouldEqual(repo.Branches["master"]);
                branch.AheadBy.ShouldEqual(2);
                branch.BehindBy.ShouldEqual(2);
            }
        }

        [Fact]
        public void CanWalkCommitsFromAnotherBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var master = repo.Branches["test"];
                master.Commits.Count().ShouldEqual(2);
            }
        }

        [Fact]
        public void CanWalkCommitsFromBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var master = repo.Branches["master"];
                master.Commits.Count().ShouldEqual(7);
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData("refs/heads/test")]
        public void CanCheckoutAnExistingBranch(string name)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var master = repo.Branches["master"];
                master.IsCurrentRepositoryHead.ShouldBeTrue();

                var branch = repo.Branches[name];
                branch.ShouldNotBeNull();

                var test = repo.Checkout(branch);
                repo.Info.IsHeadDetached.ShouldBeFalse();

                test.IsRemote.ShouldBeFalse();
                test.IsCurrentRepositoryHead.ShouldBeTrue();
                test.ShouldEqual(repo.Head);

                master.IsCurrentRepositoryHead.ShouldBeFalse();
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData("refs/heads/test")]
        public void CanCheckoutAnExistingBranchByName(string name)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var master = repo.Branches["master"];
                master.IsCurrentRepositoryHead.ShouldBeTrue();

                var test = repo.Checkout(name);
                repo.Info.IsHeadDetached.ShouldBeFalse();

                test.IsRemote.ShouldBeFalse();
                test.IsCurrentRepositoryHead.ShouldBeTrue();
                test.ShouldEqual(repo.Head);

                master.IsCurrentRepositoryHead.ShouldBeFalse();
            }
        }

        [Theory]
        [InlineData("6dcf9bf")]
        [InlineData("refs/tags/lw")]
        public void CanCheckoutAnArbitraryCommit(string commitPointer)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var master = repo.Branches["master"];
                master.IsCurrentRepositoryHead.ShouldBeTrue();

                var detachedHead = repo.Checkout(commitPointer);

                repo.Info.IsHeadDetached.ShouldBeTrue();

                detachedHead.IsRemote.ShouldBeFalse();
                detachedHead.CanonicalName.ShouldEqual(detachedHead.Name);
                detachedHead.CanonicalName.ShouldEqual("(no branch)");
                detachedHead.Tip.Sha.ShouldEqual(repo.Lookup(commitPointer).Sha);

                detachedHead.ShouldEqual(repo.Head);

                master.IsCurrentRepositoryHead.ShouldBeFalse();
                detachedHead.IsCurrentRepositoryHead.ShouldBeTrue();
                repo.Head.IsCurrentRepositoryHead.ShouldBeTrue();
            }
        }

        [Fact]
        public void CheckingOutANonExistingBranchThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Checkout("i-do-not-exist"));
            }
        }

        [Fact]
        public void CheckingOutABranchWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Checkout(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(Branch)));
                Assert.Throws<ArgumentNullException>(() => repo.Checkout(default(string)));
            }
        }

        [Fact]
        public void DeletingABranchWhichIsTheCurrentHeadThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Delete(repo.Head.Name));
            }
        }

        [Fact]
        public void DeletingABranchWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Delete(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Delete(null));
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

                head.ShouldNotBeNull();
            }
        }

        [Fact]
        public void TwoBranchesPointingAtTheSameCommitAreNotBothCurrent()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var master = repo.Branches["refs/heads/master"];

                var newBranch = repo.Branches.Create("clone-of-master", master.Tip.Sha);
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();
            }
        }

        [Fact]
        public void CanMoveABranch()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Branches["br3"].ShouldBeNull();

                var newBranch = repo.Branches.Move("br2", "br3");
                newBranch.Name.ShouldEqual("br3");

                repo.Branches["br2"].ShouldBeNull();
                repo.Branches["br3"].ShouldNotBeNull();
            }
        }

        [Fact]
        public void BlindlyMovingABranchOverAnExistingOneThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Move("br2", "test"));
            }
        }

        [Fact]
        public void CanMoveABranchWhileOverwritingAnExistingOne()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var test = repo.Branches["test"];
                test.ShouldNotBeNull();

                var br2 = repo.Branches["br2"];
                br2.ShouldNotBeNull();

                var newBranch = repo.Branches.Move("br2", "test", true);
                newBranch.Name.ShouldEqual("test");

                repo.Branches["br2"].ShouldBeNull();

                var newTest = repo.Branches["test"];
                newTest.ShouldNotBeNull();
                newTest.ShouldEqual(newBranch);

                newTest.Tip.ShouldEqual(br2.Tip);
            }
        }
    }
}
