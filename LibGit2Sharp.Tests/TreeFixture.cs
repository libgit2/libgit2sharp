﻿using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class TreeFixture : BaseFixture
    {
        private const string sha = "581f9824ecaf824221bd36edf5430f2739a7c4f5";

        [Fact]
        public void CanCompareTwoTreeEntries()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry1 = tree["README"];
                TreeEntry treeEntry2 = tree["README"];
                Assert.Equal(treeEntry2, treeEntry1);
                Assert.True((treeEntry1 == treeEntry2));
            }
        }

        [Fact]
        public void CanConvertEntryToBlob()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["README"];

                var blob = treeEntry.Target as Blob;
                Assert.NotNull(blob);
            }
        }

        [Fact]
        public void CanConvertEntryToTree()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["1"];

                var subtree = treeEntry.Target as Tree;
                Assert.NotNull(subtree);
            }
        }

        [Fact]
        public void CanEnumerateBlobs()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                Assert.Equal(3, tree.Blobs.Count());
            }
        }

        [Fact]
        public void CanEnumerateSubTrees()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                Assert.Equal(1, tree.Trees.Count());
            }
        }

        [Fact]
        public void CanEnumerateTreeEntries()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                Assert.Equal(tree.Count, tree.Count());

                Assert.Equal(new[] { "1", "README", "branch_file.txt", "new.txt" }, tree.Select(te => te.Name).ToArray());
            }
        }

        [Fact]
        public void CanGetEntryByName()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["README"];
                Assert.Equal("a8233120f6ad708f843d861ce2b7228ec4e3dec6", treeEntry.Target.Sha);
                Assert.Equal("README", treeEntry.Name);
            }
        }

        [Fact]
        public void GettingAnUknownTreeEntryReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                TreeEntry treeEntry = tree["I-do-not-exist"];
                Assert.Null(treeEntry);
            }
        }

        [Fact]
        public void CanGetEntryCountFromTree()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                Assert.Equal(4, tree.Count);
            }
        }

        [Fact]
        public void CanReadEntryAttributes()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                Assert.Equal(Mode.NonExecutableFile, tree["README"].Mode);
            }
        }

        [Fact]
        public void CanReadTheTreeData()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                Assert.NotNull(tree);
            }
        }

        [Fact]
        public void TreeDataIsPresent()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                IGitObject tree = repo.Lookup(sha);
                Assert.NotNull(tree);
            }
        }

        [Fact]
        public void CanRetrieveTreeEntryPath()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                /* From a commit tree */
                var commitTree = repo.Lookup<Commit>("4c062a6").Tree;

                TreeEntry treeTreeEntry = commitTree["1"];
                Assert.Equal("1", treeTreeEntry.Path);

                string completePath = Path.Combine("1", "branch_file.txt");

                TreeEntry blobTreeEntry = commitTree["1/branch_file.txt"];
                Assert.Equal(completePath, blobTreeEntry.Path);

                // A tree entry is now fetched through a relative path to the 
                // tree but exposes a complete path through its Path property
                var subTree = treeTreeEntry.Target as Tree;
                Assert.NotNull(subTree);
                TreeEntry anInstance = subTree["branch_file.txt"];

                Assert.NotEqual("branch_file.txt", anInstance.Path);
                Assert.Equal(completePath, anInstance.Path);
                Assert.Equal(completePath, subTree.First().Path);

                /* From a random tree */
                var tree = repo.Lookup<Tree>(treeTreeEntry.Target.Id);
                TreeEntry anotherInstance = tree["branch_file.txt"];
                Assert.Equal("branch_file.txt", anotherInstance.Path);

                Assert.Equal(tree, subTree);
                Assert.Equal(anotherInstance, anInstance);
                Assert.NotEqual(anotherInstance.Path, anInstance.Path);
                Assert.NotSame(anotherInstance, anInstance);
            }
        }
    }
}
