using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffTreeToTargetFixture : BaseFixture
    {
        private static void SetUpSimpleDiffContext(Repository repo)
        {
            var fullpath = Path.Combine(repo.Info.WorkingDirectory, "file.txt");
            File.WriteAllText(fullpath, "hello\n");

            repo.Index.Stage(fullpath);
            repo.Commit("Initial commit", DummySignature, DummySignature);

            File.AppendAllText(fullpath, "world\n");

            repo.Index.Stage(fullpath);

            File.AppendAllText(fullpath, "!!!\n");
        }

        [Fact]
        /*
         * No direct git equivalent but should output
         *
         * diff --git a/file.txt b/file.txt
         * index ce01362..4f125e3 100644
         * --- a/file.txt
         * +++ b/file.txt
         * @@ -1 +1,3 @@
         *  hello
         * +world
         * +!!!
         */
        public void CanCompareASimpleTreeAgainstTheWorkDir()
        {
            var scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                SetUpSimpleDiffContext(repo);

                TreeChanges changes = repo.Diff.Compare(repo.Head.Tip.Tree, DiffTarget.WorkingDirectory);

                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..4f125e3 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,3 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n")
                    .Append("+!!!\n");

                Assert.Equal(expected.ToString(), changes.Patch);
            }
        }

        [Fact]
        /*
         * $ git diff HEAD
         * diff --git a/file.txt b/file.txt
         * index ce01362..4f125e3 100644
         * --- a/file.txt
         * +++ b/file.txt
         * @@ -1 +1,3 @@
         *  hello
         * +world
         * +!!!
         */
        public void CanCompareASimpleTreeAgainstTheWorkDirAndTheIndex()
        {
            var scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                SetUpSimpleDiffContext(repo);

                TreeChanges changes = repo.Diff.Compare(repo.Head.Tip.Tree, DiffTarget.BothWorkingDirectoryAndIndex);

                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..4f125e3 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,3 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n")
                    .Append("+!!!\n");

                Assert.Equal(expected.ToString(), changes.Patch);
            }
        }


        [Fact]
        /*
         * $ git diff
         *
         * $ git diff HEAD
         * diff --git a/file.txt b/file.txt
         * deleted file mode 100644
         * index ce01362..0000000
         * --- a/file.txt
         * +++ /dev/null
         * @@ -1 +0,0 @@
         * -hello
         *
         * $ git diff --cached
         * diff --git a/file.txt b/file.txt
         * deleted file mode 100644
         * index ce01362..0000000
         * --- a/file.txt
         * +++ /dev/null
         * @@ -1 +0,0 @@
         * -hello
         */
        public void ShowcaseTheDifferenceBetweenTheTwoKindOfComparison()
        {
            var scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                SetUpSimpleDiffContext(repo);

                var fullpath = Path.Combine(repo.Info.WorkingDirectory, "file.txt");
                File.Move(fullpath, fullpath + ".bak");
                repo.Index.Stage(fullpath);
                File.Move(fullpath + ".bak", fullpath);

                FileStatus state = repo.Index.RetrieveStatus("file.txt");
                Assert.Equal(FileStatus.Removed | FileStatus.Untracked, state);


                TreeChanges wrkDirToIdxToTree = repo.Diff.Compare(repo.Head.Tip.Tree, DiffTarget.BothWorkingDirectoryAndIndex);
                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("deleted file mode 100644\n")
                    .Append("index ce01362..0000000\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ /dev/null\n")
                    .Append("@@ -1 +0,0 @@\n")
                    .Append("-hello\n");

                Assert.Equal(expected.ToString(), wrkDirToIdxToTree.Patch);

                TreeChanges wrkDirToTree = repo.Diff.Compare(repo.Head.Tip.Tree, DiffTarget.WorkingDirectory);
                expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..4f125e3 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,3 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n")
                    .Append("+!!!\n");

                Assert.Equal(expected.ToString(), wrkDirToTree.Patch);
            }
        }

        [Fact]
        /*
         * $ git diff --cached
         * diff --git a/file.txt b/file.txt
         * index ce01362..94954ab 100644
         * --- a/file.txt
         * +++ b/file.txt
         * @@ -1 +1,2 @@
         *  hello
         * +world
         */
        public void CanCompareASimpleTreeAgainstTheIndex()
        {
            var scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                SetUpSimpleDiffContext(repo);

                TreeChanges changes = repo.Diff.Compare(repo.Head.Tip.Tree, DiffTarget.Index);

                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..94954ab 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,2 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n");

                Assert.Equal(expected.ToString(), changes.Patch);
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
        public void CanCompareAMoreComplexTreeAgainstTheIndex()
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

        /*
         * $ git diff --cached -- "deleted_staged_file.txt" "1/branch_file.txt" "I-do/not-exist"
         * diff --git a/deleted_staged_file.txt b/deleted_staged_file.txt
         * deleted file mode 100644
         * index 5605472..0000000
         * --- a/deleted_staged_file.txt
         * +++ /dev/null
         * @@ -1 +0,0 @@
         * -things
         */
        [Fact]
        public void CanCompareASubsetofTheTreeAgainstTheIndex()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(tree, DiffTarget.Index, new[] { "deleted_staged_file.txt", "1/branch_file.txt", "I-do/not-exist" });
                Assert.NotNull(changes);

                Assert.Equal(1, changes.Count());
                Assert.Equal("deleted_staged_file.txt", changes.Deleted.Single().Path);
            }
        }

        [Fact]
        /*
         * $ git init .
         * $ echo -ne 'a' > file.txt
         * $ git add .
         * $ git commit -m "No line ending"
         * $ echo -ne '\n' >> file.txt
         * $ git add .
         * $ git diff --cached
         * diff --git a/file.txt b/file.txt
         * index 2e65efe..7898192 100644
         * --- a/file.txt
         * +++ b/file.txt
         * @@ -1 +1 @@
         * -a
         * \ No newline at end of file
         * +a
         */
        public void CanCopeWithEndOfFileNewlineChanges()
        {
            var scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.RootedDirectoryPath))
            {
                var fullpath = Path.Combine(repo.Info.WorkingDirectory, "file.txt");
                File.WriteAllText(fullpath, "a");

                repo.Index.Stage("file.txt");
                repo.Commit("Add file without line ending", DummySignature, DummySignature);

                File.AppendAllText(fullpath, "\n");
                repo.Index.Stage("file.txt");

                TreeChanges changes = repo.Diff.Compare(repo.Head.Tip.Tree, DiffTarget.Index);
                Assert.Equal(1, changes.Modified.Count());
                Assert.Equal(1, changes.LinesAdded);
                Assert.Equal(1, changes.LinesDeleted);

                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index 2e65efe..7898192 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1 @@\n")
                    .Append("-a\n")
                    .Append("\\ No newline at end of file\n")
                    .Append("+a\n");

                Assert.Equal(expected.ToString(), changes.Patch);
            }
        }
    }
}
