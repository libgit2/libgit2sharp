﻿using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class TreeDefinitionFixture : BaseFixture
    {
        /*
         * $ git ls-tree -r HEAD
         * 100755 blob 45b983be36b73c0788dc9cbcb76cbb80fc7bb057    1/branch_file.txt
         * 100644 blob a8233120f6ad708f843d861ce2b7228ec4e3dec6    README
         * 100644 blob 45b983be36b73c0788dc9cbcb76cbb80fc7bb057    branch_file.txt
         * 100644 blob a71586c1dfe8a71c6cbf6c129f404c5642ff31bd    new.txt
         */

        [Fact]
        public void CanBuildATreeDefinitionFromATree()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td);
            }
        }

        [Fact]
        public void BuildingATreeDefinitionWithBadParamsThrows()
        {
            Assert.Throws<ArgumentNullException>(() => TreeDefinition.From(null));
        }

        [Fact]
        public void RequestingANonExistingEntryReturnsNull()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);

                Assert.Null(td["nope"]);
                Assert.Null(td["not/here"]);
                Assert.Null(td["neither/in/here"]);
                Assert.Null(td["README/is/a-Blob/not-a-Tree"]);
            }
        }

        [Fact]
        public void RequestingAnEntryWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);

                Assert.Throws<ArgumentNullException>(() => td[null]);
                Assert.Throws<ArgumentException>(() => td[string.Empty]);
                Assert.Throws<ArgumentException>(() => td["/"]);
                Assert.Throws<ArgumentException>(() => td["/a"]);
                Assert.Throws<ArgumentException>(() => td["1//branch_file.txt"]);
                Assert.Throws<ArgumentException>(() => td["README/"]);
                Assert.Throws<ArgumentException>(() => td["1/"]);
            }
        }

        [Theory]
        [InlineData("1/branch_file.txt", "100755", GitObjectType.Blob, "45b983be36b73c0788dc9cbcb76cbb80fc7bb057")]
        [InlineData("README",            "100644", GitObjectType.Blob, "a8233120f6ad708f843d861ce2b7228ec4e3dec6")]
        [InlineData("branch_file.txt",   "100644", GitObjectType.Blob, "45b983be36b73c0788dc9cbcb76cbb80fc7bb057")]
        [InlineData("new.txt",           "100644", GitObjectType.Blob, "a71586c1dfe8a71c6cbf6c129f404c5642ff31bd")]
        [InlineData("1",                 "040000", GitObjectType.Tree, "7f76480d939dc401415927ea7ef25c676b8ddb8f")]
        public void CanRetrieveEntries(string path, string expectedAttributes, GitObjectType expectedType, string expectedSha)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);

                TreeEntryDefinition ted = td[path];

                Assert.Equal(ToMode(expectedAttributes), ted.Mode);
                Assert.Equal(expectedType, ted.Type);
                Assert.Equal(new ObjectId(expectedSha), ted.TargetId);
            }
        }

        //TODO: Convert Mode to a static class and add this helper method as 'FromString()'
        private static Mode ToMode(string expectedAttributes)
        {
            return (Mode)Convert.ToInt32(expectedAttributes, 8);
        }

        [Theory]
        [InlineData("README", "README_TOO")]
        [InlineData("README", "1/README")]
        [InlineData("1/branch_file.txt", "1/another_one.txt")]
        [InlineData("1/branch_file.txt", "another_one.txt")]
        [InlineData("1/branch_file.txt", "1/2/another_one.txt")]
        public void CanAddAnExistingTreeEntryDefinition(string sourcePath, string targetPath)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Null(td[targetPath]);

                TreeEntryDefinition ted = td[sourcePath];
                td.Add(targetPath, ted);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(ted, fetched);
            }
        }

        [Theory]
        [InlineData("a8233120f6ad708f843d861ce2b7228ec4e3dec6", "README_TOO")]
        [InlineData("a8233120f6ad708f843d861ce2b7228ec4e3dec6", "1/README")]
        [InlineData("45b983be36b73c0788dc9cbcb76cbb80fc7bb057", "1/another_one.txt")]
        [InlineData("45b983be36b73c0788dc9cbcb76cbb80fc7bb057", "another_one.txt")]
        public void CanAddAnExistingBlob(string blobSha, string targetPath)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Null(td[targetPath]);

                var objectId = new ObjectId(blobSha);
                var blob = repo.Lookup<Blob>(objectId);

                td.Add(targetPath, blob, Mode.NonExecutableFile);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(Mode.NonExecutableFile, fetched.Mode);
            }
        }

        [Fact]
        public void CanAddAnExistingTree()
        {
            const string treeSha = "7f76480d939dc401415927ea7ef25c676b8ddb8f";
            const string targetPath = "1/2";

            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);

                var objectId = new ObjectId(treeSha);
                var tree = repo.Lookup<Tree>(objectId);

                td.Add(targetPath, tree);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(Mode.Directory, fetched.Mode);

                Assert.NotNull(td["1/2/branch_file.txt"]);
            }
        }

        [Fact]
        public void CanReplaceAnExistingTreeWithABlob()
        {
            const string blobSha = "a8233120f6ad708f843d861ce2b7228ec4e3dec6";
            const string targetPath = "1";

            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Equal(GitObjectType.Tree, td[targetPath].Type);

                var objectId = new ObjectId(blobSha);
                var blob = repo.Lookup<Blob>(objectId);

                Assert.NotNull(td["1/branch_file.txt"]);

                td.Add(targetPath, blob, Mode.NonExecutableFile);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(GitObjectType.Blob, td[targetPath].Type);
                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(Mode.NonExecutableFile, fetched.Mode);

                Assert.Null(td["1/branch_file.txt"]);
            }
        }

        [Theory]
        [InlineData("README")]
        [InlineData("1/branch_file.txt")]
        public void CanReplaceAnExistingBlobWithATree(string targetPath)
        {
            const string treeSha = "7f76480d939dc401415927ea7ef25c676b8ddb8f";

            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td[targetPath]);
                Assert.Equal(GitObjectType.Blob, td[targetPath].Type);

                var objectId = new ObjectId(treeSha);
                var tree = repo.Lookup<Tree>(objectId);

                td.Add(targetPath, tree);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(GitObjectType.Tree, td[targetPath].Type);
                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(Mode.Directory, fetched.Mode);
            }
        }

        [Fact]
        public void CanNotReplaceAnExistingTreeWithATreeBeingAssembled()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Equal(GitObjectType.Tree, td["1"].Type);

                td.Add("new/one", repo.Lookup<Blob>("a823312"), Mode.NonExecutableFile)
                    .Add("new/two", repo.Lookup<Blob>("a71586c"), Mode.NonExecutableFile)
                    .Add("new/tree", repo.Lookup<Tree>("7f76480"));

                Assert.Throws<InvalidOperationException>(() => td.Add("1", td["new"]));
            }
        }

        [Fact]
        public void ModifyingTheContentOfATreeSetsItsOidToNull()
        {
            const string blobSha = "a8233120f6ad708f843d861ce2b7228ec4e3dec6";
            const string targetPath = "1/another_branch_file.txt";

            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);

                var objectId = new ObjectId(blobSha);
                var blob = repo.Lookup<Blob>(objectId);

                Assert.NotEqual(ObjectId.Zero, td["1"].TargetId);

                td.Add(targetPath, blob, Mode.NonExecutableFile);

                Assert.Equal(ObjectId.Zero, td["1"].TargetId);
            }
        }

        [Fact]
        public void CanAddAnExistingBlobEntryWithAnExistingTree()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                TreeEntryDefinition original = td["README"];

                td.Add("1/2/README", original);

                TreeEntryDefinition fetched = td["1/2/README"];
                Assert.NotNull(fetched);

                Assert.Equal(original.TargetId, fetched.TargetId);
                Assert.Equal(original.Mode, fetched.Mode);

                Assert.NotNull(td["1/branch_file.txt"]);
            }
        }
    }
}
