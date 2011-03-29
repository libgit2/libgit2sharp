using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class BranchFixture
    {
        private readonly List<string> expectedBranches = new List<string> {"packed-test", "packed", "br2", "master", "test"};

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
        public void CanLookupLocalBranch()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var master = repo.Branches["master"];
                master.ShouldNotBeNull();
                master.Type.ShouldEqual(BranchType.Local);
                master.Name.ShouldEqual("master");
                master.Reference.Name.ShouldEqual("refs/heads/master");
                master.Reference.Target.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
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
                master.Commits.Count().ShouldEqual(6);
            }
        }
    }
}