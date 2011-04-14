﻿using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class TreeFixture
    {
        private const string sha = "f1ab0b7ba5a5691b76a5715cffd58c21e9149bee";

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
                var count = tree.GetCount();
                Assert.That(count, Is.EqualTo(4));
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
                Assert.That(tree[1].Sha, Is.EqualTo("a8233120f6ad708f843d861ce2b7228ec4e3dec6"));
            }
        }

        [Test]
        public void CanGetNameFromTreeEntry()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tree = repo.Lookup<Tree>(sha);
                Assert.That(tree[1].Name, Is.EqualTo("README"));
            }
        }
    }
}