using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffBlobToBlobFixture : BaseFixture
    {
        [Fact]
        public void ComparingABlobAgainstItselfReturnsNoDifference()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("7909961");

                ContentChanges changes = repo.Diff.Compare(blob, blob);

                Assert.Equal(0, changes.LinesAdded);
                Assert.Equal(0, changes.LinesDeleted);
                Assert.Equal(string.Empty, changes.Patch);
            }
        }

        [Fact]
        public void CanCompareTwoVersionsOfABlobWithADiffOfTwoHunks()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var oldblob = repo.Lookup<Blob>("7909961");
                var newblob = repo.Lookup<Blob>("4e935b7");

                ContentChanges changes = repo.Diff.Compare(oldblob, newblob);

                Assert.False(changes.IsBinaryComparison);

                Assert.Equal(3, changes.LinesAdded);
                Assert.Equal(1, changes.LinesDeleted);

                var expected = new StringBuilder()
                 .Append("@@ -1,4 +1,5 @@\n")
                 .Append(" 1\n")
                 .Append("+2\n")
                 .Append(" 3\n")
                 .Append(" 4\n")
                 .Append(" 5\n")
                 .Append("@@ -8,8 +9,9 @@\n")
                 .Append(" 8\n")
                 .Append(" 9\n")
                 .Append(" 10\n")
                 .Append("-12\n")
                 .Append("+11\n")
                 .Append(" 12\n")
                 .Append(" 13\n")
                 .Append(" 14\n")
                 .Append(" 15\n")
                 .Append("+16\n");

                Assert.Equal(expected.ToString(), changes.Patch);
            }
        }

        Blob CreateBinaryBlob(IRepository repo)
        {
            string fullpath = Path.Combine(repo.Info.WorkingDirectory, "binary.bin");

            File.WriteAllBytes(fullpath, new byte[] { 17, 16, 0, 4, 65 });

            return repo.ObjectDatabase.CreateBlob(fullpath);
        }

        [Fact]
        public void CanCompareATextualBlobAgainstABinaryBlob()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Blob binBlob = CreateBinaryBlob(repo);

                var blob = repo.Lookup<Blob>("7909961");

                ContentChanges changes = repo.Diff.Compare(blob, binBlob);

                Assert.True(changes.IsBinaryComparison);

                Assert.Equal(0, changes.LinesAdded);
                Assert.Equal(0, changes.LinesDeleted);
            }
        }

        [Fact]
        public void CanCompareABlobAgainstANullBlob()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("7909961");

                ContentChanges changes = repo.Diff.Compare(null, blob);

                Assert.NotEqual(0, changes.LinesAdded);
                Assert.Equal(0, changes.LinesDeleted);
                Assert.NotEqual(string.Empty, changes.Patch);

                changes = repo.Diff.Compare(blob, null);

                Assert.Equal(0, changes.LinesAdded);
                Assert.NotEqual(0, changes.LinesDeleted);
                Assert.NotEqual(string.Empty, changes.Patch);
            }
        }

        [Fact]
        public void ComparingTwoNullBlobsReturnsAnEmptyContentChanges()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                ContentChanges changes = repo.Diff.Compare((Blob)null, (Blob)null);

                Assert.False(changes.IsBinaryComparison);

                Assert.Equal(0, changes.LinesAdded);
                Assert.Equal(0, changes.LinesDeleted);
            }
        }
    }
}
