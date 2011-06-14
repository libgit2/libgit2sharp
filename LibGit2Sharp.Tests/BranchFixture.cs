using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class BranchFixture : BaseFixture
    {
        private readonly List<string> expectedBranches = new List<string> {"packed-test", "packed", "br2", "master", "test", "deadbeef"};

        [Test]
        public void CanCheckoutAnExistingBranch()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var master = repo.Branches["master"];
                master.IsCurrentRepositoryHead.ShouldBeTrue();

                var test = repo.Branches.Checkout("test");

                test.IsCurrentRepositoryHead.ShouldBeTrue();
                master.IsCurrentRepositoryHead.ShouldBeFalse();
            }
        }

        [Test]
        public void CanCreateBranch()
        {
            using (var path = new TemporaryCloneOfTestRepo())
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

        [Test]
        public void CanCreateBranchFromImplicitHead()
        {
            using (var path = new TemporaryCloneOfTestRepo())
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

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanCreateBranchFromExplicitHead()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var newBranch = repo.CreateBranch(name, "HEAD");
                newBranch.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
            }
        }

        [Test]
        public void CanListAllBranches()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var r in repo.Branches)
                {
                    Assert.Contains(r.Name, expectedBranches);
                }

                repo.Branches.Count().ShouldEqual(6);
            }
        }

        [Test]
        public void CanLookupABranchByItsCanonicalName()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
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

        [Test]
        public void CanLookupLocalBranch()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
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

        [Test]
        public void CanWalkCommitsFromAnotherBranch()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var master = repo.Branches["test"];
                master.Commits.Count().ShouldEqual(2);
            }
        }

        [Test]
        public void CanWalkCommitsFromBranch()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var master = repo.Branches["master"];
                master.Commits.Count().ShouldEqual(7);
            }
        }

        [Test]
        public void CheckoutBranchWithBadParamsThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Checkout(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Checkout(null));
            }
        }

        [Test]
        public void CreatingBranchWithEmptyNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Create(string.Empty, repo.Head.CanonicalName));
            }
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CreatingBranchWithUnknownNamedTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Create("my_new_branch", "my_old_branch"));
            }
        }

        [Test]
        public void CreatingBranchWithUnknownShaTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Branches.Create("my_new_branch", Constants.UnknownSha));
            }
        }

        [Test]
        public void CreatingBranchWithEmptyTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Create("bad_branch", string.Empty));
            }
        }

        [Test]
        public void CreatingBranchWithNullNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create(null, repo.Head.CanonicalName));
            }
        }

        [Test]
        public void CreatingBranchWithNullTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create("bad_branch", null));
            }
        }

        [Test]
        public void DeletingBranchWithBadParamsThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Delete(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Delete(null));
            }
        }

        [Test]
        public void OnlyOneBranchIsTheHead()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Branch head = null;

                foreach (var branch in repo.Branches)
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
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var master = repo.Branches["refs/heads/master"];

                var newBranch = repo.Branches.Create("clone-of-master", master.Tip.Sha);
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();
            }
        }

        [Test]
        public void CanMoveABranch()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Branches["br3"].ShouldBeNull();

                var newBranch = repo.Branches.Move("br2", "br3");
                newBranch.Name.ShouldEqual("br3");

                repo.Branches["br2"].ShouldBeNull();
                repo.Branches["br3"].ShouldNotBeNull();
            }
        }

        [Test]
        public void BlindlyMovingABranchOverAnExistingOneThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Branches.Move("br2", "test"));
            }
        }

        [Test]
        public void CanMoveABranchWhileOverwritingAnExistingOne()
        {
            using (var path = new TemporaryCloneOfTestRepo())
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
        
        [Test]
        public void CreatingABranchTriggersTheCreationOfADirectReference()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newBranch = repo.CreateBranch("clone-of-master");
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();
                
                var commitId = repo.Head.Tip.Id;
                newBranch.Tip.Id.ShouldEqual(commitId);

                var reference = repo.Refs[newBranch.CanonicalName];
                reference.ShouldNotBeNull();
                Assert.IsInstanceOf(typeof (DirectReference), reference);
            }
        }
    }
}
