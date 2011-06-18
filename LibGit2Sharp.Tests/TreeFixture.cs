using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class TreeFixture : BaseFixture
    {
        private const string sha = "581f9824ecaf824221bd36edf5430f2739a7c4f5";

        [Test]
        public void CanCompareTwoTreeEntries()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry1 = tree["README"];
                TreeEntry treeEntry2 = tree["README"];
                treeEntry1.ShouldEqual(treeEntry2);
                (treeEntry1 == treeEntry2).ShouldBeTrue();
            }
        }

        [Test]
        public void CanConvertEntryToBlob()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["README"];

                var blob = treeEntry.Target as Blob;
                blob.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanConvertEntryToTree()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["1"];

                var subtree = treeEntry.Target as Tree;
                subtree.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanEnumerateBlobs()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.Files.Count().ShouldEqual(3);
            }
        }

        [Test]
        public void CanEnumerateSubTrees()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.Trees.Count().ShouldEqual(1);
            }
        }

        [Test]
        public void CanEnumerateTreeEntries()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.Count().ShouldEqual(tree.Count);

                CollectionAssert.AreEquivalent(new[] {"1", "README", "branch_file.txt", "new.txt"}, tree.Select(te => te.Name).ToArray());
            }
        }

        [Test]
        public void CanGetEntryByName()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["README"];
                treeEntry.Target.Sha.ShouldEqual("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                treeEntry.Name.ShouldEqual("README");
            }
        }

        [Test]
        public void CanGetEntryCountFromTree()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.Count.ShouldEqual(4);
            }
        }

        [Test]
        public void CanReadEntryAttributes()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree["README"].Attributes.ShouldEqual(33188);
            }
        }

        [Test]
        public void CanReadTheTreeData()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.ShouldNotBeNull();
            }
        }

        [Test]
        public void TreeDataIsPresent()
        {
            using (var repo = new Repository(Constants.BareTestRepoPath))
            {
                var tree = repo.Lookup(sha);
                tree.ShouldNotBeNull();
            }
        }
    }
}