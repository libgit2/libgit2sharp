using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffTreeToTargetFixture : BaseFixture
    {
        [Fact(Skip = "Unfinished")]
        public void CanCompareATreeAgainstTheWorkDir()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(tree, DiffTarget.WorkingDirectory);

                Assert.NotNull(changes);
            }
        }

        /*
         * $ git diff --cached
         * diff --git a/deleted_staged_file.txt b/deleted_staged_file.txt
         * deleted file mode 100644
         * index 5605472..0000000
         * --- a/deleted_staged_file.txt
         * +++ /dev/null
         * @@ -1 +0,0 @@
         * -things
         * diff --git a/modified_staged_file.txt b/modified_staged_file.txt
         * index 15d2ecc..e68bcc7 100644
         * --- a/modified_staged_file.txt
         * +++ b/modified_staged_file.txt
         * @@ -1 +1,2 @@
         * +a change
         *  more files!
         * diff --git a/new_tracked_file.txt b/new_tracked_file.txt
         * new file mode 100644
         * index 0000000..935a81d
         * --- /dev/null
         * +++ b/new_tracked_file.txt
         * @@ -0,0 +1 @@
         * +a new file
         */
        [Fact]
        public void CanCompareATreeAgainstTheIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(tree, DiffTarget.Index);
                Assert.NotNull(changes);

                Assert.Equal(3, changes.Count());
                Assert.Equal("deleted_staged_file.txt", changes.Deleted.Single().Path);
                Assert.Equal("new_tracked_file.txt", changes.Added.Single().Path);
                Assert.Equal("modified_staged_file.txt", changes.Modified.Single().Path);
            }
        }

        [Fact(Skip = "Buggy?")]
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
        public void CanCompareATreeAgainstTheWorkDirAndTheIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(tree, DiffTarget.BothWorkingDirectoryAndIndex);

                Assert.NotNull(changes);
            }
        }
    }
}
