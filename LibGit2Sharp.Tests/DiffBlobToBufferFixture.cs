using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffBlobToBufferFixture : BaseFixture
    {
        [Fact]
        public void CompareABlobAndAString()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                // This is CanCompareTwoVersionsOfABlobWithADiffOfTwoHunks() but we read the second
                // one into memory and pretend we read it from disk or our editor.
                var oldblob = repo.Lookup<Blob>("7909961");
                var newblob = repo.Lookup<Blob>("4e935b7");

                var newContent = newblob.GetContentText();

                ContentChanges changes = repo.Diff.Compare(oldblob, newContent);

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
    }
}

