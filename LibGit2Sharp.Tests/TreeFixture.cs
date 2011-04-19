using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class TreeFixture
    {
        private const string sha = "581f9824ecaf824221bd36edf5430f2739a7c4f5";

        [Test]
        public void TreeDataIsPresent()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup(sha);
                tree.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanReadTheTreeData()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanGetEntryCountFromTree()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.Count.ShouldEqual(4);
            }
        }

        [Test]
        public void CanGetEntryByIndex()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree[0].ShouldNotBeNull();
                tree[1].ShouldNotBeNull();
            }
        }

        [Test]
        public void CanGetShaFromTreeEntry()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree[1].Target.Sha.ShouldEqual("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
            }
        }

        [Test]
        public void CanGetNameFromTreeEntry()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree[1].Name.ShouldEqual("README");
            }
        }

        [Test]
        public void CanGetEntryByName()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree["README"].Target.Sha.ShouldEqual("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
            }
        }

        [Test]
        public void CanConvertEntryToBlob()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["README"];

                var blob = treeEntry.Object as Blob;
                blob.ShouldNotBeNull();

                treeEntry.Blob.ShouldEqual(blob);

                var subTree = treeEntry.Object as Tree;
                subTree.ShouldBeNull();
            }
        }

        [Test]
        public void CanReadEntryAttributes()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree["README"].Attributes.ShouldEqual(33188);
            }
        }

        [Test]
        public void CanConvertEntryToTree()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree[0];

                var subtree = treeEntry.Object as Tree;
                subtree.ShouldNotBeNull();

                //treeEntry.Tree.ShouldEqual(subtree);

                var blob = treeEntry.Object as Blob;
                blob.ShouldBeNull();
            }
        }

        [Test]
        public void CanEnumerateTree()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                tree.Count().ShouldEqual(tree.Count);

                CollectionAssert.AreEquivalent(new[]{"1", "README", "branch_file.txt", "new.txt"}, tree.Select(te => te.Name).ToArray());
            }
        }
    }
}