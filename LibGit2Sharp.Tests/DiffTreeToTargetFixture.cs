using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffTreeToTargetFixture : BaseFixture
    {
        private static void SetUpSimpleDiffContext(IRepository repo)
        {
            var fullpath = Touch(repo.Info.WorkingDirectory, "file.txt", "hello\n");

            repo.Stage(fullpath);
            repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

            File.AppendAllText(fullpath, "world\n");

            repo.Stage(fullpath);

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
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                SetUpSimpleDiffContext(repo);

                var changes = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree,
                    DiffTargets.WorkingDirectory);
                Assert.Equal(1, changes.Modified.Count());

                var patch = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree,
                    DiffTargets.WorkingDirectory);
                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..4f125e3 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,3 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n")
                    .Append("+!!!\n");

                Assert.Equal(expected.ToString(), patch);
            }
        }

        [Fact]
        public void CanCompareAMoreComplexTreeAgainstTheWorkdir()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;

                var changes = repo.Diff.Compare<TreeChanges>(tree, DiffTargets.WorkingDirectory);
                Assert.NotNull(changes);

                Assert.Equal(6, changes.Count());

                Assert.Equal(new[] { "deleted_staged_file.txt", "deleted_unstaged_file.txt" },
                    changes.Deleted.Select(tec => tec.Path));

                Assert.Equal(new[] { "new_tracked_file.txt", "new_untracked_file.txt" },
                    changes.Added.Select(tec => tec.Path));

                Assert.Equal(new[] { "modified_staged_file.txt", "modified_unstaged_file.txt" },
                    changes.Modified.Select(tec => tec.Path));
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
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                SetUpSimpleDiffContext(repo);

                var changes = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree,
                    DiffTargets.Index | DiffTargets.WorkingDirectory);
                Assert.Equal(1, changes.Modified.Count());

                var patch = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree,
                    DiffTargets.Index | DiffTargets.WorkingDirectory);
                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..4f125e3 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,3 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n")
                    .Append("+!!!\n");

                Assert.Equal(expected.ToString(), patch);
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
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                SetUpSimpleDiffContext(repo);

                var fullpath = Path.Combine(repo.Info.WorkingDirectory, "file.txt");
                File.Move(fullpath, fullpath + ".bak");
                repo.Stage(fullpath);
                File.Move(fullpath + ".bak", fullpath);

                FileStatus state = repo.RetrieveStatus("file.txt");
                Assert.Equal(FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, state);

                var wrkDirToIdxToTree = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree,
                    DiffTargets.Index | DiffTargets.WorkingDirectory);

                Assert.Equal(1, wrkDirToIdxToTree.Deleted.Count());
                Assert.Equal(0, wrkDirToIdxToTree.Modified.Count());

                var patch = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree,
                    DiffTargets.Index | DiffTargets.WorkingDirectory);
                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("deleted file mode 100644\n")
                    .Append("index ce01362..0000000\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ /dev/null\n")
                    .Append("@@ -1 +0,0 @@\n")
                    .Append("-hello\n");

                Assert.Equal(expected.ToString(), patch);

                var wrkDirToTree = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree,
                    DiffTargets.WorkingDirectory);

                Assert.Equal(0, wrkDirToTree.Deleted.Count());
                Assert.Equal(1, wrkDirToTree.Modified.Count());

                patch = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree,
                    DiffTargets.WorkingDirectory);
                expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..4f125e3 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,3 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n")
                    .Append("+!!!\n");

                Assert.Equal(expected.ToString(), patch);
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
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                SetUpSimpleDiffContext(repo);

                var changes = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree,
                    DiffTargets.Index);
                Assert.Equal(1, changes.Modified.Count());

                var patch = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree,
                    DiffTargets.Index);
                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index ce01362..94954ab 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1,2 @@\n")
                    .Append(" hello\n")
                    .Append("+world\n");

                Assert.Equal(expected.ToString(), patch);
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;

                var changes = repo.Diff.Compare<TreeChanges>(tree, DiffTargets.Index);
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;

                var changes = repo.Diff.Compare<TreeChanges>(tree, DiffTargets.Index,
                    new[] { "deleted_staged_file.txt", "1/branch_file.txt" });

                Assert.NotNull(changes);

                Assert.Equal(1, changes.Count());
                Assert.Equal("deleted_staged_file.txt", changes.Deleted.Single().Path);
            }
        }

        private static void AssertCanCompareASubsetOfTheTreeAgainstTheIndex(TreeChanges changes)
        {
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count());
            Assert.Equal("deleted_staged_file.txt", changes.Deleted.Single().Path);
        }

        [Fact]
        public void CanCompareASubsetofTheTreeAgainstTheIndexWithLaxExplicitPathsValidationAndANonExistentPath()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;

                var changes = repo.Diff.Compare<TreeChanges>(tree, DiffTargets.Index,
                    new[] { "deleted_staged_file.txt", "1/branch_file.txt", "I-do/not-exist" }, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false });
                AssertCanCompareASubsetOfTheTreeAgainstTheIndex(changes);

                changes = repo.Diff.Compare<TreeChanges>(tree, DiffTargets.Index,
                    new[] { "deleted_staged_file.txt", "1/branch_file.txt", "I-do/not-exist" });
                AssertCanCompareASubsetOfTheTreeAgainstTheIndex(changes);
            }
        }

        [Fact]
        public void ComparingASubsetofTheTreeAgainstTheIndexWithStrictExplicitPathsValidationAndANonExistentPathThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;

                Assert.Throws<UnmatchedPathException>(() => repo.Diff.Compare<TreeChanges>(tree, DiffTargets.Index,
                    new[] { "deleted_staged_file.txt", "1/branch_file.txt", "I-do/not-exist" }, new ExplicitPathsOptions()));
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
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                var fullpath = Touch(repo.Info.WorkingDirectory, "file.txt", "a");

                repo.Stage("file.txt");
                repo.Commit("Add file without line ending", Constants.Signature, Constants.Signature);

                File.AppendAllText(fullpath, "\n");
                repo.Stage("file.txt");

                var changes = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.Index);
                Assert.Equal(1, changes.Modified.Count());

                var patch = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree, DiffTargets.Index);
                var expected = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index 2e65efe..7898192 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1 +1 @@\n")
                    .Append("-a\n")
                    .Append("\\ No newline at end of file\n")
                    .Append("+a\n");

                Assert.Equal(expected.ToString(), patch);
                Assert.Equal(1, patch.LinesAdded);
                Assert.Equal(1, patch.LinesDeleted);
            }
        }

        [Fact]
        public void ComparingATreeInABareRepositoryAgainstTheWorkDirOrTheIndexThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<BareRepositoryException>(
                    () => repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.WorkingDirectory));
                Assert.Throws<BareRepositoryException>(
                    () => repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.Index));
                Assert.Throws<BareRepositoryException>(
                    () => repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree, DiffTargets.WorkingDirectory | DiffTargets.Index));
            }
        }

        [Fact]
        public void CanCompareANullTreeAgainstTheIndex()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                SetUpSimpleDiffContext(repo);

                var changes = repo.Diff.Compare<TreeChanges>(null,
                    DiffTargets.Index);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Added.Count());

                Assert.Equal("file.txt", changes.Added.Single().Path);
            }
        }

        [Fact]
        public void CanCompareANullTreeAgainstTheWorkdir()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                SetUpSimpleDiffContext(repo);

                var changes = repo.Diff.Compare<TreeChanges>(null,
                    DiffTargets.WorkingDirectory);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Added.Count());

                Assert.Equal("file.txt", changes.Added.Single().Path);
            }
        }

        [Fact]
        public void CanCompareANullTreeAgainstTheWorkdirAndTheIndex()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                SetUpSimpleDiffContext(repo);

                var changes = repo.Diff.Compare<TreeChanges>(null,
                    DiffTargets.WorkingDirectory | DiffTargets.Index);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Added.Count());

                Assert.Equal("file.txt", changes.Added.Single().Path);
            }
        }
    }
}
