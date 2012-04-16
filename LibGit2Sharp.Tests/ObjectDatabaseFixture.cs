using System.IO;
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
    }
}
