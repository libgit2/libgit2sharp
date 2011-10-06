using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class BranchFixture : BaseFixture
    {
        private readonly string[] expectedBranches = new[] { "br2", "master", "packed", "packed-test", "test", };

        [Test]
        public void CanCheckoutAnExistingBranch()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["master"];
                master.IsCurrentRepositoryHead.ShouldBeTrue();

                Branch test = repo.Branches.Checkout("test");
                repo.Info.IsHeadDetached.ShouldBeFalse();

                test.IsCurrentRepositoryHead.ShouldBeTrue();
                master.IsCurrentRepositoryHead.ShouldBeFalse();
            }
        }

        [Test]
        public void CheckingOutANonExistingBranchThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Checkout("i-do-not-exist"));
            }
        }

        [Test]
        public void CanCreateBranch()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                newBranch.ShouldNotBeNull();
                newBranch.Name.ShouldEqual(name);
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.Tip.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                repo.Branches.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();

                repo.Branches.Delete(newBranch.Name);
            }
        }

        [Test]
        public void CanCreateBranchUsingAbbreviatedSha()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name, "be3563a");
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.Tip.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
            }
        }

        [Test]
        public void CanCreateBranchFromImplicitHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name);
                newBranch.ShouldNotBeNull();
                newBranch.Name.ShouldEqual(name);
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();
                newBranch.Tip.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                repo.Branches.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();
            }
        }

        [Test]
        public void CanCreateBranchFromExplicitHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name, "HEAD");
                newBranch.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
            }
        }

        [Test]
        public void CreatingABranchFromATagPeelsToTheCommit()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "i-peel-tag";
                Branch newBranch = repo.CreateBranch(name, "refs/tags/test");
                newBranch.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("e90810b8df3e80c413d903f631643c716887138d");
            }
        }

        [Test]
        public void CreatingABranchFromANonCommitObjectThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                const string name = "sorry-dude-i-do-not-do-blobs-nor-trees";
                Assert.Throws<LibGit2Exception>(() => repo.CreateBranch(name, "refs/tags/point_to_blob"));
                Assert.Throws<LibGit2Exception>(() => repo.CreateBranch(name, "53fc32d"));
                Assert.Throws<LibGit2Exception>(() => repo.CreateBranch(name, "0266163"));
            }
        }

        [Test]
        public void GetBranchByNameWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Branch branch;
                Assert.Throws<ArgumentNullException>(() => branch = repo.Branches[null]);
                Assert.Throws<ArgumentException>(() => branch = repo.Branches[""]);
            }
        }

        [Test]
        public void CanListAllBranches()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                CollectionAssert.AreEqual(expectedBranches, repo.Branches.Select(b => b.Name).ToArray());

                repo.Branches.Count().ShouldEqual(5);
            }
        }

        [Test]
        public void CanListAllBranchesIncludingRemoteRefs()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var expectedBranchesIncludingRemoteRefs = new[]
                                                              {
                                                                  new { Name = "master", Sha = "592d3c869dbc4127fc57c189cb94f2794fa84e7e" },
                                                                  new { Name = "origin/HEAD", Sha = "4c062a6361ae6959e06292c1fa5e2822d9c96345" },
                                                                  new { Name = "origin/br2", Sha = "a4a7dce85cf63874e984719f4fdd239f5145052f" },
                                                                  new { Name = "origin/master", Sha = "4c062a6361ae6959e06292c1fa5e2822d9c96345" },
                                                                  new { Name = "origin/packed-test", Sha = "4a202b346bb0fb0db7eff3cffeb3c70babbd2045" },
                                                                  new { Name = "origin/test", Sha = "e90810b8df3e80c413d903f631643c716887138d" },
                                                              };
                CollectionAssert.AreEqual(expectedBranchesIncludingRemoteRefs, repo.Branches.Select(b => new { b.Name, b.Tip.Sha }).ToArray());
            }
        }

        [Test]
        public void CanLookupABranchByItsCanonicalName()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Branch branch = repo.Branches["refs/heads/br2"];
                branch.ShouldNotBeNull();
                branch.Name.ShouldEqual("br2");

                Branch branch2 = repo.Branches["refs/heads/br2"];
                branch2.ShouldNotBeNull();
                branch2.Name.ShouldEqual("br2");

                branch2.ShouldEqual(branch);
                (branch2 == branch).ShouldBeTrue();
            }
        }

        [Test]
        public void CanLookupLocalBranch()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                master.ShouldNotBeNull();
                master.IsRemote.ShouldBeFalse();
                master.Name.ShouldEqual("master");
                master.CanonicalName.ShouldEqual("refs/heads/master");
                master.IsCurrentRepositoryHead.ShouldBeTrue();
                master.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
            }
        }

        [Test]
        public void CanWalkCommitsFromAnotherBranch()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Branch master = repo.Branches["test"];
                master.Commits.Count().ShouldEqual(2);
            }
        }

        [Test]
        public void CanWalkCommitsFromBranch()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                master.Commits.Count().ShouldEqual(7);
            }
        }

        [Test]
        public void CheckoutBranchWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Checkout(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Checkout(null));
            }
        }

        [Test]
        public void CreatingBranchWithUnknownNamedTargetThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("my_new_branch", "my_old_branch"));
            }
        }

        [Test]
        public void CreatingBranchWithUnknownShaTargetThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("my_new_branch", Constants.UnknownSha));
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("my_new_branch", Constants.UnknownSha.Substring(0, 7)));
            }
        }

        [Test]
        public void CreatingABranchPointingAtANonCanonicalReferenceThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Create("nocanonicaltarget", "br2"));
            }
        }

        [Test]
        public void CreatingBranchWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create(null, repo.Head.CanonicalName));
                Assert.Throws<ArgumentException>(() => repo.Branches.Create(string.Empty, repo.Head.CanonicalName));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create("bad_branch", null));
                Assert.Throws<ArgumentException>(() => repo.Branches.Create("bad_branch", string.Empty));
            }
        }

        [Test]
        public void DeletingABranchWhichIsTheCurrentHeadThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Delete(repo.Head.Name));
            }
        }

        [Test]
        public void DeletingABranchWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Delete(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Delete(null));
            }
        }

        [Test]
        public void OnlyOneBranchIsTheHead()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
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

                    Assert.Fail("Both '{0}' and '{1}' appear to be Head.", head.CanonicalName, branch.CanonicalName);
                }

                head.ShouldNotBeNull();
            }
        }

        [Test]
        public void TwoBranchesPointingAtTheSameCommitAreNotBothCurrent()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch master = repo.Branches["refs/heads/master"];

                Branch newBranch = repo.Branches.Create("clone-of-master", master.Tip.Sha);
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();
            }
        }

        [Test]
        public void CanMoveABranch()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Branches["br3"].ShouldBeNull();

                Branch newBranch = repo.Branches.Move("br2", "br3");
                newBranch.Name.ShouldEqual("br3");

                repo.Branches["br2"].ShouldBeNull();
                repo.Branches["br3"].ShouldNotBeNull();
            }
        }

        [Test]
        public void BlindlyMovingABranchOverAnExistingOneThrows()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Branches.Move("br2", "test"));
            }
        }

        [Test]
        public void CanMoveABranchWhileOverwritingAnExistingOne()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch test = repo.Branches["test"];
                test.ShouldNotBeNull();

                Branch br2 = repo.Branches["br2"];
                br2.ShouldNotBeNull();

                Branch newBranch = repo.Branches.Move("br2", "test", true);
                newBranch.Name.ShouldEqual("test");

                repo.Branches["br2"].ShouldBeNull();

                Branch newTest = repo.Branches["test"];
                newTest.ShouldNotBeNull();
                newTest.ShouldEqual(newBranch);

                newTest.Tip.ShouldEqual(br2.Tip);
            }
        }

        [Test]
        public void CreatingABranchTriggersTheCreationOfADirectReference()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Branch newBranch = repo.CreateBranch("clone-of-master");
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();

                ObjectId commitId = repo.Head.Tip.Id;
                newBranch.Tip.Id.ShouldEqual(commitId);

                Reference reference = repo.Refs[newBranch.CanonicalName];
                reference.ShouldNotBeNull();
                Assert.IsInstanceOf(typeof(DirectReference), reference);
            }
        }
    }
}
