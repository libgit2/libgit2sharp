using System;
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td);
            }
        }

        [Fact]
        public void BuildingATreeDefinitionWithBadParamsThrows()
        {
            Assert.Throws<ArgumentNullException>(() => TreeDefinition.From(default(Tree)));
        }

        [Fact]
        public void RequestingANonExistingEntryReturnsNull()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
        [InlineData("1/branch_file.txt", "100755", TreeEntryTargetType.Blob, "45b983be36b73c0788dc9cbcb76cbb80fc7bb057")]
        [InlineData("README", "100644", TreeEntryTargetType.Blob, "a8233120f6ad708f843d861ce2b7228ec4e3dec6")]
        [InlineData("branch_file.txt", "100644", TreeEntryTargetType.Blob, "45b983be36b73c0788dc9cbcb76cbb80fc7bb057")]
        [InlineData("new.txt", "100644", TreeEntryTargetType.Blob, "a71586c1dfe8a71c6cbf6c129f404c5642ff31bd")]
        [InlineData("1", "040000", TreeEntryTargetType.Tree, "7f76480d939dc401415927ea7ef25c676b8ddb8f")]
        public void CanRetrieveEntries(string path, string expectedAttributes, TreeEntryTargetType expectedType, string expectedSha)
        {
            string repoPath = SandboxBareTestRepo();
            using (var repo = new Repository(repoPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);

                TreeEntryDefinition ted = td[path];

                Assert.Equal(ToMode(expectedAttributes), ted.Mode);
                Assert.Equal(expectedType, ted.TargetType);
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
        [InlineData("README", "1/2/README")]
        [InlineData("1/branch_file.txt", "1/another_one.txt")]
        [InlineData("1/branch_file.txt", "another_one.txt")]
        [InlineData("1/branch_file.txt", "1/2/another_one.txt")]
        [InlineData("1/branch_file.txt", "1/2/3/another_one.txt")]
        [InlineData("1", "2")]
        [InlineData("1", "2/3")]
        public void CanAddAnExistingTreeEntryDefinition(string sourcePath, string targetPath)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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

        [Fact]
        public void CanAddAnExistingGitLinkTreeEntryDefinition()
        {
            const string sourcePath = "sm_unchanged";
            const string targetPath = "sm_from_td";

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
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

        private const string StringOf40Chars = "0123456789012345678901234567890123456789";

        /// <summary>
        /// Used to verify that windows path limitation to 260 chars is not limiting the size of
        /// the keys present in the object database.
        /// </summary>
        private const string StringOf600Chars =
            StringOf40Chars + StringOf40Chars + StringOf40Chars + StringOf40Chars + StringOf40Chars
            + StringOf40Chars + StringOf40Chars + StringOf40Chars + StringOf40Chars + StringOf40Chars
            + StringOf40Chars + StringOf40Chars + StringOf40Chars + StringOf40Chars + StringOf40Chars;

        [Theory]
        [InlineData("README", "README_TOO")]
        [InlineData("README", "1/README")]
        [InlineData("README", "1/2/README")]
        [InlineData("1/branch_file.txt", "1/another_one.txt")]
        [InlineData("1/branch_file.txt", "another_one.txt")]
        [InlineData("1/branch_file.txt", "1/2/another_one.txt")]
        [InlineData("1/branch_file.txt", "1/2/3/another_one.txt")]
        [InlineData("1", "2")]
        [InlineData("1", "2/3")]
        [InlineData("1", "C:\\/10")]
        [InlineData("1", " : * ? \" < > |")]
        [InlineData("1", StringOf600Chars)]
        public void CanAddAndRemoveAnExistingTreeEntry(string sourcePath, string targetPath)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var tree = repo.Head.Tip.Tree;
                var td = TreeDefinition.From(tree);
                Assert.Null(td[targetPath]);

                var te = tree[sourcePath];
                td.Add(targetPath, te);

                var fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(te.Target.Id, fetched.TargetId);

                // Ensuring that the object database can handle uncommon paths.
                var newTree = repo.ObjectDatabase.CreateTree(td);
                Assert.Equal(newTree[targetPath].Target.Id, te.Target.Id);

                td.Remove(targetPath);
                Assert.Null(td[targetPath]);
            }
        }

        [Theory]
        [InlineData("C:\\")]
        [InlineData(" : * ? \" \n < > |")]
        [InlineData("a\\b")]
        [InlineData("\\\\b\a")]
        [InlineData("éàµ")]
        [InlineData(StringOf600Chars)]
        public void TreeNamesCanContainCharsForbiddenOnSomeOS(string targetName)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var pointedItem = repo.Head.Tip.Tree;

                var td = new TreeDefinition();
                td.Add(targetName, pointedItem);

                var newTree = repo.ObjectDatabase.CreateTree(td);
                Assert.Equal(newTree[targetName].Target.Sha, pointedItem.Sha);
                Assert.Equal(newTree[targetName].Name, targetName);
            }
        }

        [Theory]
        [InlineData("sm_from_td")]
        [InlineData("1/sm_from_td")]
        [InlineData("1/2/sm_from_td")]
        public void CanAddAnExistingGitLinkTreeEntry(string targetPath)
        {
            const string sourcePath = "sm_unchanged";

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var tree = repo.Head.Tip.Tree;
                var td = TreeDefinition.From(tree);
                Assert.Null(td[targetPath]);

                var te = tree[sourcePath];
                td.Add(targetPath, te);

                var fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(te.Target.Id, fetched.TargetId);
            }
        }

        [Theory]
        [InlineData("a8233120f6ad708f843d861ce2b7228ec4e3dec6", "README_TOO")]
        [InlineData("a8233120f6ad708f843d861ce2b7228ec4e3dec6", "1/README")]
        [InlineData("45b983be36b73c0788dc9cbcb76cbb80fc7bb057", "1/another_one.txt")]
        [InlineData("45b983be36b73c0788dc9cbcb76cbb80fc7bb057", "another_one.txt")]
        public void CanAddAnExistingBlob(string blobSha, string targetPath)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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

        [Theory]
        [InlineData("a8233120f6ad708f843d861ce2b7228ec4e3dec6", "README_TOO")]
        [InlineData("a8233120f6ad708f843d861ce2b7228ec4e3dec6", "1/README")]
        [InlineData("45b983be36b73c0788dc9cbcb76cbb80fc7bb057", "1/another_one.txt")]
        [InlineData("45b983be36b73c0788dc9cbcb76cbb80fc7bb057", "another_one.txt")]
        public void CanAddBlobById(string blobSha, string targetPath)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Null(td[targetPath]);

                var objectId = new ObjectId(blobSha);

                td.Add(targetPath, objectId, Mode.NonExecutableFile);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(Mode.NonExecutableFile, fetched.Mode);
            }
        }

        [Fact]
        public void CannotAddTreeById()
        {
            const string treeSha = "7f76480d939dc401415927ea7ef25c676b8ddb8f";
            const string targetPath = "1/2";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Null(td[targetPath]);

                var objectId = new ObjectId(treeSha);

                Assert.Throws<ArgumentException>(() => td.Add(targetPath, objectId, Mode.Directory));
            }
        }

        [Fact]
        public void CanAddAnExistingSubmodule()
        {
            const string submodulePath = "sm_unchanged";

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submodulePath];
                Assert.NotNull(submodule);

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td[submodulePath]);

                td.Remove(submodulePath);
                Assert.Null(td[submodulePath]);

                td.Add(submodule);

                TreeEntryDefinition fetched = td[submodulePath];
                Assert.NotNull(fetched);

                Assert.Equal(submodule.HeadCommitId, fetched.TargetId);
                Assert.Equal(TreeEntryTargetType.GitLink, fetched.TargetType);
                Assert.Equal(Mode.GitLink, fetched.Mode);
            }
        }

        [Fact]
        public void CanAddAnExistingTree()
        {
            const string treeSha = "7f76480d939dc401415927ea7ef25c676b8ddb8f";
            const string targetPath = "1/2";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Equal(TreeEntryTargetType.Tree, td[targetPath].TargetType);

                var objectId = new ObjectId(blobSha);
                var blob = repo.Lookup<Blob>(objectId);

                Assert.NotNull(td["1/branch_file.txt"]);

                td.Add(targetPath, blob, Mode.NonExecutableFile);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(TreeEntryTargetType.Blob, td[targetPath].TargetType);
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

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td[targetPath]);
                Assert.Equal(TreeEntryTargetType.Blob, td[targetPath].TargetType);

                var objectId = new ObjectId(treeSha);
                var tree = repo.Lookup<Tree>(objectId);

                td.Add(targetPath, tree);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(TreeEntryTargetType.Tree, td[targetPath].TargetType);
                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(Mode.Directory, fetched.Mode);
            }
        }

        [Fact]
        public void CanReplaceAnExistingTreeWithAGitLink()
        {
            var commitId = (ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0";
            const string targetPath = "just_a_dir";

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Equal(TreeEntryTargetType.Tree, td[targetPath].TargetType);

                Assert.NotNull(td["just_a_dir/contents"]);

                td.AddGitLink(targetPath, commitId);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(commitId, fetched.TargetId);
                Assert.Equal(TreeEntryTargetType.GitLink, fetched.TargetType);
                Assert.Equal(Mode.GitLink, fetched.Mode);

                Assert.Null(td["just_a_dir/contents"]);
            }
        }

        [Fact]
        public void CanReplaceAnExistingGitLinkWithATree()
        {
            const string treeSha = "607d96653d4d0a4f733107f7890c2e67b55b620d";
            const string targetPath = "sm_unchanged";

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td[targetPath]);
                Assert.Equal(TreeEntryTargetType.GitLink, td[targetPath].TargetType);
                Assert.Equal(Mode.GitLink, td[targetPath].Mode);

                var objectId = new ObjectId(treeSha);
                var tree = repo.Lookup<Tree>(objectId);

                td.Add(targetPath, tree);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(TreeEntryTargetType.Tree, fetched.TargetType);
                Assert.Equal(Mode.Directory, fetched.Mode);
            }
        }

        [Fact]
        public void CanReplaceAnExistingBlobWithAGitLink()
        {
            var commitId = (ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0";
            const string targetPath = "just_a_file";

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td[targetPath]);
                Assert.Equal(TreeEntryTargetType.Blob, td[targetPath].TargetType);

                td.AddGitLink(targetPath, commitId);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(TreeEntryTargetType.GitLink, td[targetPath].TargetType);
                Assert.Equal(commitId, fetched.TargetId);
                Assert.Equal(Mode.GitLink, fetched.Mode);
            }
        }

        [Fact]
        public void CanReplaceAnExistingGitLinkWithABlob()
        {
            const string blobSha = "42cfb95cd01bf9225b659b5ee3edcc78e8eeb478";
            const string targetPath = "sm_unchanged";

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.NotNull(td[targetPath]);
                Assert.Equal(TreeEntryTargetType.GitLink, td[targetPath].TargetType);
                Assert.Equal(Mode.GitLink, td[targetPath].Mode);

                var objectId = new ObjectId(blobSha);
                var blob = repo.Lookup<Blob>(objectId);

                td.Add(targetPath, blob, Mode.NonExecutableFile);

                TreeEntryDefinition fetched = td[targetPath];
                Assert.NotNull(fetched);

                Assert.Equal(objectId, fetched.TargetId);
                Assert.Equal(TreeEntryTargetType.Blob, fetched.TargetType);
                Assert.Equal(Mode.NonExecutableFile, fetched.Mode);
            }
        }

        [Fact]
        public void CanNotReplaceAnExistingTreeWithATreeBeingAssembled()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Equal(TreeEntryTargetType.Tree, td["1"].TargetType);

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

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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

        [Fact]
        public void CanRemoveADirectoryWithChildren()
        {
            const string blobSha = "a8233120f6ad708f843d861ce2b7228ec4e3dec6";
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                TreeDefinition td = new TreeDefinition();
                var blob = repo.Lookup<Blob>(blobSha);
                td.Add("folder/subfolder/file1", blob, Mode.NonExecutableFile);
                td.Add("folder/file1", blob, Mode.NonExecutableFile);
                td.Remove("folder");
                Assert.Null(td["folder"]);
                Tree t = repo.ObjectDatabase.CreateTree(td);
                Assert.Null(t["folder"]);
            }
        }
    }
}
