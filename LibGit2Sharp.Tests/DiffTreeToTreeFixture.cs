using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffTreeToTreeFixture : BaseFixture
    {
        private static readonly string subBranchFilePath = Path.Combine("1", "branch_file.txt");

        [Fact]
        public void ComparingATreeAgainstItselfReturnsNoDifference()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(tree, tree);

                Assert.Empty(changes);
                Assert.Equal(string.Empty, changes.Patch);
            }
        }

        [Fact]
        public void RetrievingANonExistentFileChangeReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(tree, tree);

                Assert.Null(changes["batman"]);
            }
        }

        /*
         * $ git diff --stat HEAD^..HEAD
         *  1.txt |    1 +
         *  1 file changed, 1 insertion(+)
         */
        [Fact]
        public void CanCompareACommitTreeAgainstItsParent()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree parentCommitTree = repo.Head.Tip.Parents.Single().Tree;

                TreeChanges changes = repo.Diff.Compare(parentCommitTree, commitTree);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Added.Count());

                TreeEntryChanges treeEntryChanges = changes["1.txt"];
                Assert.False(treeEntryChanges.IsBinaryComparison);

                Assert.Equal("1.txt", treeEntryChanges.Path);
                Assert.Equal(ChangeKind.Added, treeEntryChanges.Status);

                Assert.Equal(treeEntryChanges, changes.Added.Single());
                Assert.Equal(1, treeEntryChanges.LinesAdded);

                Assert.Equal(Mode.Nonexistent, treeEntryChanges.OldMode);
            }
        }

        /*
         * $ git diff 9fd738e..HEAD -- "1" "2/"
         * diff --git a/1/branch_file.txt b/1/branch_file.txt
         * new file mode 100755
         * index 0000000..45b983b
         * --- /dev/null
         * +++ b/1/branch_file.txt
         * @@ -0,0 +1 @@
         * +hi
         */
        [Fact]
        public void CanCompareASubsetofTheTreeAgainstOneOfItsAncestor()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;
                Tree ancestor = repo.Lookup<Commit>("9fd738e").Tree;

                TreeChanges changes = repo.Diff.Compare(ancestor, tree, new[]{ "1", "2/" });
                Assert.NotNull(changes);

                Assert.Equal(1, changes.Count());
                Assert.Equal(subBranchFilePath, changes.Added.Single().Path);
            }
        }

        /*
         * $ git diff --stat origin/test..HEAD
         *  1.txt                      |    1 +
         *  1/branch_file.txt          |    1 +
         *  README                     |    1 +
         *  branch_file.txt            |    1 +
         *  deleted_staged_file.txt    |    1 +
         *  deleted_unstaged_file.txt  |    1 +
         *  modified_staged_file.txt   |    1 +
         *  modified_unstaged_file.txt |    1 +
         *  new.txt                    |    1 +
         *  readme.txt                 |    2 --
         *  10 files changed, 9 insertions(+), 2 deletions(-)
         */
        [Fact]
        public void CanCompareACommitTreeAgainstATreeWithNoCommonAncestor()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree commitTreeWithDifferentAncestor = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(commitTreeWithDifferentAncestor, commitTree);

                Assert.Equal(10, changes.Count());
                Assert.Equal(9, changes.Added.Count());
                Assert.Equal(1, changes.Deleted.Count());

                Assert.Equal("readme.txt", changes.Deleted.Single().Path);
                Assert.Equal(new[] { "1.txt", subBranchFilePath, "README", "branch_file.txt", "deleted_staged_file.txt", "deleted_unstaged_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new.txt" },
                             changes.Added.Select(x => x.Path));

                Assert.Equal(9, changes.LinesAdded);
                Assert.Equal(2, changes.LinesDeleted);
                Assert.Equal(2, changes["readme.txt"].LinesDeleted);
            }
        }

        /*
         * $ git diff -M f8d44d7..4be51d6
         * diff --git a/my-name-does-not-feel-right.txt b/super-file.txt
         * similarity index 82%
         * rename from my-name-does-not-feel-right.txt
         * rename to super-file.txt
         * index e8953ab..16bdf1d 100644
         * --- a/my-name-does-not-feel-right.txt
         * +++ b/super-file.txt
         * @@ -2,3 +2,4 @@ That's a terrible name!
         *  I don't like it.
         *  People look down at me and laugh. :-(
         *  Really!!!!
         * +Yeah! Better!
         *
         * $ git diff -M --shortstat f8d44d7..4be51d6
         *  1 file changed, 1 insertion(+)
         */
        [Fact(Skip = "Not implemented in libgit2 yet.")]
        public void CanDetectTheRenamingOfAModifiedFile()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree commitTreeWithRenamedFile = repo.Lookup<Commit>("4be51d6").Tree;

                TreeChanges changes = repo.Diff.Compare(rootCommitTree, commitTreeWithRenamedFile);

                Assert.Equal(1, changes.Count());
                Assert.Equal("super-file.txt", changes["super-file.txt"].Path);
                Assert.Equal("my-name-does-not-feel-right.txt", changes["super-file.txt"].OldPath);
                //Assert.Equal(1, changes.FilesRenamed.Count());
            }
        }

        /*
         * $ git diff f8d44d7..ec9e401
         * diff --git a/numbers.txt b/numbers.txt
         * index 7909961..4625a36 100644
         * --- a/numbers.txt
         * +++ b/numbers.txt
         * @@ -8,8 +8,9 @@
         *  8
         *  9
         *  10
         * -12
         * +11
         *  12
         *  13
         *  14
         *  15
         * +16
         * \ No newline at end of file
         *
         * $ git diff --shortstat f8d44d7..ec9e401
         *  1 file changed, 2 insertions(+), 1 deletion(-)
         */
        [Fact]
        public void CanCompareTwoVersionsOfAFileWithATrailingNewlineDeletion()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree commitTreeWithUpdatedFile = repo.Lookup<Commit>("ec9e401").Tree;

                TreeChanges changes = repo.Diff.Compare(rootCommitTree, commitTreeWithUpdatedFile);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Modified.Count());

                TreeEntryChanges treeEntryChanges = changes.Modified.Single();

                Assert.Equal(2, treeEntryChanges.LinesAdded);
                Assert.Equal(1, treeEntryChanges.LinesDeleted);
            }
        }

        /*
         * $ git diff --inter-hunk-context=2 f8d44d7..7252fe2
         * diff --git a/my-name-does-not-feel-right.txt b/my-name-does-not-feel-right.txt
         * deleted file mode 100644
         * index e8953ab..0000000
         * --- a/my-name-does-not-feel-right.txt
         * +++ /dev/null
         * @@ -1,4 +0,0 @@
         * -That's a terrible name!
         * -I don't like it.
         * -People look down at me and laugh. :-(
         * -Really!!!!
         * diff --git a/numbers.txt b/numbers.txt
         * index 7909961..4e935b7 100644
         * --- a/numbers.txt
         * +++ b/numbers.txt
         * @@ -1,4 +1,5 @@
         *  1
         * +2
         *  3
         *  4
         *  5
         * @@ -8,8 +9,9 @@
         *  8
         *  9
         *  10
         * -12
         * +11
         *  12
         *  13
         *  14
         *  15
         * +16
         * diff --git a/super-file.txt b/super-file.txt
         * new file mode 100644
         * index 0000000..16bdf1d
         * --- /dev/null
         * +++ b/super-file.txt
         * @@ -0,0 +1,5 @@
         * +That's a terrible name!
         * +I don't like it.
         * +People look down at me and laugh. :-(
         * +Really!!!!
         * +Yeah! Better!
         *
         * $ git diff --stat f8d44d7..7252fe2
         *  my-name-does-not-feel-right.txt |    4 ----
         *  numbers.txt                     |    4 +++-
         *  super-file.txt                  |    5 +++++
         *  3 files changed, 8 insertions(+), 5 deletions(-)
         */
        [Fact]
        public void CanCompareTwoVersionsOfAFileWithADiffOfTwoHunks()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree mergedCommitTree = repo.Lookup<Commit>("7252fe2").Tree;

                TreeChanges changes = repo.Diff.Compare(rootCommitTree, mergedCommitTree);

                Assert.Equal(3, changes.Count());
                Assert.Equal(1, changes.Modified.Count());
                Assert.Equal(1, changes.Deleted.Count());
                Assert.Equal(1, changes.Added.Count());

                TreeEntryChanges treeEntryChanges = changes["numbers.txt"];

                Assert.Equal(3, treeEntryChanges.LinesAdded);
                Assert.Equal(1, treeEntryChanges.LinesDeleted);

                Assert.Equal(Mode.Nonexistent, changes["my-name-does-not-feel-right.txt"].Mode);

                var expected = new StringBuilder()
                    .Append("diff --git a/numbers.txt b/numbers.txt\n")
                    .Append("index 7909961..4e935b7 100644\n")
                    .Append("--- a/numbers.txt\n")
                    .Append("+++ b/numbers.txt\n")
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

                Assert.Equal(expected.ToString(), treeEntryChanges.Patch);

                expected = new StringBuilder()
                    .Append("diff --git a/my-name-does-not-feel-right.txt b/my-name-does-not-feel-right.txt\n")
                    .Append("deleted file mode 100644\n")
                    .Append("index e8953ab..0000000\n")
                    .Append("--- a/my-name-does-not-feel-right.txt\n")
                    .Append("+++ /dev/null\n")
                    .Append("@@ -1,4 +0,0 @@\n")
                    .Append("-That's a terrible name!\n")
                    .Append("-I don't like it.\n")
                    .Append("-People look down at me and laugh. :-(\n")
                    .Append("-Really!!!!\n")
                    .Append("diff --git a/numbers.txt b/numbers.txt\n")
                    .Append("index 7909961..4e935b7 100644\n")
                    .Append("--- a/numbers.txt\n")
                    .Append("+++ b/numbers.txt\n")
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
                    .Append("+16\n")
                    .Append("diff --git a/super-file.txt b/super-file.txt\n")
                    .Append("new file mode 100644\n")
                    .Append("index 0000000..16bdf1d\n")
                    .Append("--- /dev/null\n")
                    .Append("+++ b/super-file.txt\n")
                    .Append("@@ -0,0 +1,5 @@\n")
                    .Append("+That's a terrible name!\n")
                    .Append("+I don't like it.\n")
                    .Append("+People look down at me and laugh. :-(\n")
                    .Append("+Really!!!!\n")
                    .Append("+Yeah! Better!\n");

                Assert.Equal(expected.ToString(), changes.Patch);
            }
        }

        [Fact(Skip = "Not working against libgit2 debug version.")]
        public void CanCompareATreeAgainstANullTree()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(tree, null);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Deleted.Count());

                Assert.Equal("readme.txt", changes.Deleted.Single().Path);

                changes = repo.Diff.Compare(null, tree);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Added.Count());

                Assert.Equal("readme.txt", changes.Added.Single().Path);
            }
        }

        [Fact(Skip = "Not working against libgit2 debug version.")]
        public void ComparingTwoNullTreesReturnsAnEmptyTreeChanges()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                TreeChanges changes = repo.Diff.Compare(null, null, null);

                Assert.Equal(0, changes.Count());
            }
        }
    }
}
