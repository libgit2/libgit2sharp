using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffWorkdirToIndexFixture : BaseFixture
    {
        /*
         * $ git diff
         * diff --git a/deleted_unstaged_file.txt b/deleted_unstaged_file.txt
         * deleted file mode 100644
         * index f2e4113..0000000
         * --- a/deleted_unstaged_file.txt
         * +++ /dev/null
         * @@ -1 +0,0 @@
         * -stuff
         * diff --git a/modified_unstaged_file.txt b/modified_unstaged_file.txt
         * index 9217230..da6fd65 100644
         * --- a/modified_unstaged_file.txt
         * +++ b/modified_unstaged_file.txt
         * @@ -1 +1,2 @@
         * +some more text
         *  more files! more files!
         */
        [Fact]
        public void CanCompareTheWorkDirAgainstTheIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                TreeChanges changes = repo.Diff.Compare();

                Assert.Equal(2, changes.Count());
                Assert.Equal("deleted_unstaged_file.txt", changes.Deleted.Single().Path);
                Assert.Equal("modified_unstaged_file.txt", changes.Modified.Single().Path);
            }
        }
    }
}
