using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ObjectDatabaseFixture : BaseFixture
    {
        [Theory]
        [InlineData("8496071c1b46c854b31185ea97743be6a8774479", true)]
        [InlineData("1385f264afb75a56a5bec74243be9b367ba4ca08", true)]
        [InlineData("ce08fe4884650f067bd5703b6a59a8b3b3c99a09", false)]
        [InlineData("deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", false)]
        public void CanTellIfObjectsExists(string sha, bool shouldExists)
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var oid = new ObjectId(sha);

                Assert.Equal(shouldExists, repo.ObjectDatabase.Contains(oid));
            }
        }

        [Fact]
        public void CanCreateABlobFromAFileInTheWorkingDirectory()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus("hello.txt"));

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "hello.txt"), "I'm a new file\n");

                Blob blob = repo.ObjectDatabase.CreateBlob("hello.txt");
                Assert.NotNull(blob);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", blob.Sha);

                /* The file is unknown from the Index nor the Head ... */
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus("hello.txt"));

                /* ...however, it's indeed stored in the repository. */
                var fetchedBlob = repo.Lookup<Blob>(blob.Id);
                Assert.Equal(blob, fetchedBlob);
            }
        }

        [Fact]
        public void CanCreateABlobIntoTheDatabaseOfABareRepository()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            SelfCleaningDirectory directory = BuildSelfCleaningDirectory();

            Directory.CreateDirectory(directory.RootedDirectoryPath);
            string filepath = Path.Combine(directory.RootedDirectoryPath, "hello.txt");
            File.WriteAllText(filepath, "I'm a new file\n");

            using (var repo = new Repository(scd.RepositoryPath))
            {
                /*
                 * $ echo "I'm a new file" | git hash-object --stdin
                 * dc53d4c6b8684c21b0b57db29da4a2afea011565
                 */
                Assert.Null(repo.Lookup<Blob>("dc53d4c6b8684c21b0b57db29da4a2afea011565"));

                Blob blob = repo.ObjectDatabase.CreateBlob(filepath);

                Assert.NotNull(blob);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", blob.Sha);
                Assert.Equal("I'm a new file\n", blob.ContentAsUtf8());

                var fetchedBlob = repo.Lookup<Blob>(blob.Id);
                Assert.Equal(blob, fetchedBlob);
            }
        }

        [Theory]
        [InlineData("321cbdf08803c744082332332838df6bd160f8f9", null)]
        [InlineData("321cbdf08803c744082332332838df6bd160f8f9", "dummy.data")]
        [InlineData("e9671e138a780833cb689753570fd10a55be84fb", "dummy.txt")]
        [InlineData("e9671e138a780833cb689753570fd10a55be84fb", "dummy.guess")]
        public void CanCreateABlobFromABinaryReader(string expectedSha, string hintPath)
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                sb.Append("libgit2\n\r\n");
            }

            using (var repo = new Repository(scd.RepositoryPath))
            {
                CreateAttributesFiles(Path.Combine(repo.Info.Path, "info"), "attributes");

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
                using (var binReader = new BinaryReader(stream))
                {
                    Blob blob = repo.ObjectDatabase.CreateBlob(binReader, hintPath);
                    Assert.Equal(expectedSha, blob.Sha);
                }
            }
        }

        private static void CreateAttributesFiles(string where, string filename)
        {
            const string attributes = "* text=auto\n*.txt text\n*.data binary\n";

            Directory.CreateDirectory(where);
            File.WriteAllText(Path.Combine(where, filename), attributes);
        }

        [Theory]
        [InlineData("README")]
        [InlineData("README AS WELL")]
        [InlineData("2/README AS WELL")]
        [InlineData("1/README AS WELL")]
        [InlineData("1")]
        public void CanCreateATreeByAlteringAnExistingOne(string targetPath)
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            using (var repo = new Repository(scd.RepositoryPath))
            {
                var blob = repo.Lookup<Blob>(new ObjectId("a8233120f6ad708f843d861ce2b7228ec4e3dec6"));

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree)
                    .Add(targetPath, blob, Mode.NonExecutableFile);

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);
            }
        }

        [Fact]
        public void CanCreateATreeByRemovingEntriesFromExistingOne()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            using (var repo = new Repository(scd.RepositoryPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree)
                    .Remove("branch_file.txt")
                    .Remove("1/branch_file.txt");

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);

                Assert.Null(tree["branch_file.txt"]);
                Assert.Null(tree["1/branch_file.txt"]);
                Assert.Null(tree["1"]);

                Assert.Equal("814889a078c031f61ed08ab5fa863aea9314344d", tree.Sha);
            }
        }

        [Fact]
        public void RemovingANonExistingEntryFromATreeDefinitionHasNoSideEffect()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            using (var repo = new Repository(scd.RepositoryPath))
            {
                Tree head = repo.Head.Tip.Tree;

                TreeDefinition td = TreeDefinition.From(head)
                    .Remove("123")
                    .Remove("nope")
                    .Remove("not/here")
                    .Remove("neither/in/here")
                    .Remove("README/is/a-Blob/not-a-Tree");

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);

                Assert.Equal(head, tree);
            }
        }

        [Fact]
        public void CanCreateAnEmptyTree()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            using (var repo = new Repository(scd.RepositoryPath))
            {
                var td = new TreeDefinition();

                Tree tree = repo.ObjectDatabase.CreateTree(td);
                Assert.NotNull(tree);
                Assert.Equal("4b825dc642cb6eb9a060e54bf8d69288fbee4904", tree.Sha);
            }
        }

        [Fact]
        public void CanReplaceAnExistingTreeWithAnotherPersitedTree()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            using (var repo = new Repository(scd.RepositoryPath))
            {
                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                Assert.Equal(GitObjectType.Tree, td["1"].Type);

                TreeDefinition newTd = new TreeDefinition()
                    .Add("new/one", repo.Lookup<Blob>("a823312"), Mode.NonExecutableFile)
                    .Add("new/two", repo.Lookup<Blob>("a71586c"), Mode.NonExecutableFile)
                    .Add("new/tree", repo.Lookup<Tree>("7f76480"));

                repo.ObjectDatabase.CreateTree(newTd);

                td.Add("1", newTd["new"]);
                Assert.Equal(GitObjectType.Tree, td["1/tree"].Type);
            }
        }

        [Fact]
        public void CanCreateATreeContainingABlobFromAFileInTheWorkingDirectory()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus("hello.txt"));
                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "hello.txt"), "I'm a new file\n");

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree)
                    .Add("1/new file", "hello.txt", Mode.NonExecutableFile);

                TreeEntryDefinition ted = td["1/new file"];
                Assert.NotNull(ted);
                Assert.Equal(ObjectId.Zero, ted.TargetId);

                td.Add("1/2/another new file", ted);

                Tree tree = repo.ObjectDatabase.CreateTree(td);

                TreeEntry te = tree["1/new file"];
                Assert.NotNull(te.Target);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", te.Target.Sha);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", td["1/new file"].TargetId.Sha);

                te = tree["1/2/another new file"];
                Assert.NotNull(te.Target);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", te.Target.Sha);
                Assert.Equal("dc53d4c6b8684c21b0b57db29da4a2afea011565", td["1/2/another new file"].TargetId.Sha);
            }
        }

        [Fact]
        public void CanCreateACommit()
        {
            TemporaryCloneOfTestRepo scd = BuildTemporaryCloneOfTestRepo();

            using (var repo = new Repository(scd.RepositoryPath))
            {
                Branch head = repo.Head;

                TreeDefinition td = TreeDefinition.From(repo.Head.Tip.Tree);
                td.Add("1/2/readme", td["README"]);

                Tree tree = repo.ObjectDatabase.CreateTree(td);

                Commit commit = repo.ObjectDatabase.CreateCommit("message", DummySignature, DummySignature, tree, new[] { repo.Head.Tip });

                Branch newHead = repo.Head;

                Assert.Equal(head, newHead);
                Assert.Equal(commit, repo.Lookup<Commit>(commit.Sha));
            }
        }
    }
}
