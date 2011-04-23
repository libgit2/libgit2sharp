using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class BranchFixture
    {
        private readonly List<string> expectedBranches = new List<string> {"packed-test", "packed", "br2", "master", "test"};

        [Test]
        public void CanCreateBranch()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var newBranch = repo.Branches.Create(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                newBranch.ShouldNotBeNull();
                newBranch.Name.ShouldEqual(name);
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.Tip.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                repo.Branches.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();

                repo.Refs.Delete(newBranch.CanonicalName); //TODO: To be replaced with repo.Branches.Delete(newBranch.Name)
            }
        }

        [Test]
        public void CanCreateBranchFromAnotherBranch()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "unit_test";
                var newBranch = repo.Branches.Create(name, "master");
                newBranch.ShouldNotBeNull();
                newBranch.Name.ShouldEqual(name);
                newBranch.CanonicalName.ShouldEqual("refs/heads/" + name);
                newBranch.IsCurrentRepositoryHead.ShouldBeFalse();
                newBranch.Tip.ShouldNotBeNull();
                newBranch.Tip.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                repo.Branches.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();

                repo.Refs.Delete(newBranch.CanonicalName); //TODO: To be replaced with repo.Branches.Delete(newBranch.Name)
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

                repo.Branches.Count().ShouldEqual(5);
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
        public void CreatingBranchWithEmptyNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Create(string.Empty, "not_important_sha"));
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
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create(null, "not_important_sha"));
            }
        }

        [Test]
        public void CreatingBranchWithNullTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create("bad_branch", (string) null));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Create("bad_branch", (ObjectId) null));
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
    }
}