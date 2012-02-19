using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class TreeBuilderFixture : BaseFixture
    {
        [Test]
        public void RebuildTree()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var builder = new TreeBuilder();
                builder.ShouldNotBeNull();
                foreach(TreeEntry entry in repo.Head.Tip.Tree) {
                    builder.Insert(entry);
                }

                var result = builder.Write(repo);
                result.Id.ShouldEqual(repo.Head.Tip.Tree.Id);
            }
        }
    }
}

