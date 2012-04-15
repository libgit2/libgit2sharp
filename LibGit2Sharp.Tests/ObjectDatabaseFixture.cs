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
    }
}
