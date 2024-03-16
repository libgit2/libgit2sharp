using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class DiffTreeToTreeFixture : BaseFixture
    {
        private static readonly string subBranchFilePath = string.Join("/", "1", "branch_file.txt");

        [Fact]
        public void ComparingATreeAgainstItselfReturnsNoDifference()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(tree, tree))
                {
                    Assert.Empty(changes);
                }

                using (var patch = repo.Diff.Compare<Patch>(tree, tree))
                {
                    Assert.Empty(patch);
                    Assert.Equal(string.Empty, patch);
                }
            }
        }

        [Fact]
        public void RetrievingANonExistentFileChangeReturnsNull()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(tree, tree))
                {
                    Assert.Equal(0, changes.Count(c => c.Path == "batman"));
                }
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree parentCommitTree = repo.Head.Tip.Parents.Single().Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(parentCommitTree, commitTree))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Added);

                    TreeEntryChanges treeEntryChanges = changes.Single(c => c.Path == "1.txt");

                    Assert.Equal("1.txt", treeEntryChanges.Path);
                    Assert.Equal(ChangeKind.Added, treeEntryChanges.Status);

                    Assert.Equal(treeEntryChanges.Path, changes.Added.Single().Path);

                    Assert.Equal(Mode.Nonexistent, treeEntryChanges.OldMode);
                }

                using (var patch = repo.Diff.Compare<Patch>(parentCommitTree, commitTree))
                {
                    Assert.False(patch["1.txt"].IsBinaryComparison);
                }
            }
        }

        static void CreateBinaryFile(string path)
        {
            var content = new byte[] { 0x1, 0x0, 0x2, 0x0 };

            using (var binfile = File.Create(path))
            {
                for (int i = 0; i < 1000; i++)
                {
                    binfile.Write(content, 0, content.Length);
                }
            }
        }

        [Fact]
        public void CanDetectABinaryChange()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                const string filename = "binfile.foo";
                var filepath = Path.Combine(repo.Info.WorkingDirectory, filename);

                CreateBinaryFile(filepath);

                Commands.Stage(repo, filename);
                var commit = repo.Commit("Add binary file", Constants.Signature, Constants.Signature);

                File.AppendAllText(filepath, "abcdef");

                using (var patch = repo.Diff.Compare<Patch>(commit.Tree, DiffTargets.WorkingDirectory, new[] { filename }))
                    Assert.True(patch[filename].IsBinaryComparison);

                Commands.Stage(repo, filename);
                var commit2 = repo.Commit("Update binary file", Constants.Signature, Constants.Signature);

                using (var patch2 = repo.Diff.Compare<Patch>(commit.Tree, commit2.Tree, new[] { filename }))
                    Assert.True(patch2[filename].IsBinaryComparison);
            }
        }

        [Fact]
        public void CanDetectABinaryDeletion()
        {
            using (var repo = new Repository(SandboxStandardTestRepo()))
            {
                const string filename = "binfile.foo";
                var filepath = Path.Combine(repo.Info.WorkingDirectory, filename);

                CreateBinaryFile(filepath);

                Commands.Stage(repo, filename);
                var commit = repo.Commit("Add binary file", Constants.Signature, Constants.Signature);

                File.Delete(filepath);

                using (var patch = repo.Diff.Compare<Patch>(commit.Tree, DiffTargets.WorkingDirectory, new[] { filename }))
                    Assert.True(patch[filename].IsBinaryComparison);

                Commands.Remove(repo, filename);
                var commit2 = repo.Commit("Delete binary file", Constants.Signature, Constants.Signature);

                using (var patch2 = repo.Diff.Compare<Patch>(commit.Tree, commit2.Tree, new[] { filename }))
                    Assert.True(patch2[filename].IsBinaryComparison);
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Head.Tip.Tree;
                Tree ancestor = repo.Lookup<Commit>("9fd738e").Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(ancestor, tree, new[] { "1" }))
                {
                    Assert.NotNull(changes);

                    Assert.Single(changes);
                    Assert.Equal(subBranchFilePath, changes.Added.Single().Path);
                }
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree commitTreeWithDifferentAncestor = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree))
                {
                    Assert.Equal(10, changes.Count());
                    Assert.Equal(9, changes.Added.Count());
                    Assert.Single(changes.Deleted);

                    Assert.Equal("readme.txt", changes.Deleted.Single().Path);
                    Assert.Equal(new[] { "1.txt", subBranchFilePath, "README", "branch_file.txt", "deleted_staged_file.txt", "deleted_unstaged_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new.txt" },
                                 changes.Added.Select(x => x.Path).OrderBy(p => p, StringComparer.Ordinal).ToArray());
                }

                using (var patch = repo.Diff.Compare<Patch>(commitTreeWithDifferentAncestor, commitTree))
                {
                    Assert.Equal(9, patch.LinesAdded);
                    Assert.Equal(2, patch.LinesDeleted);
                    Assert.Equal(2, patch["readme.txt"].LinesDeleted);
                }
            }
        }

        [Fact]
        public void CanCompareATreeAgainstAnotherTreeWithLaxExplicitPathsValidationAndNonExistentPath()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree commitTreeWithDifferentAncestor = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                        new[] { "if-I-exist-this-test-is-really-unlucky.txt" }, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false }))
                {
                    Assert.Empty(changes);
                }

                using (var changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                    new[] { "if-I-exist-this-test-is-really-unlucky.txt" }))
                {
                    Assert.Empty(changes);
                }
            }
        }

        [Fact]
        public void ComparingATreeAgainstAnotherTreeWithStrictExplicitPathsValidationThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree commitTreeWithDifferentAncestor = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                Assert.Throws<UnmatchedPathException>(() =>
                    repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                        new[] { "if-I-exist-this-test-is-really-unlucky.txt" }, new ExplicitPathsOptions()));
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
        [Fact]
        public void DetectsTheRenamingOfAModifiedFileByDefault()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree commitTreeWithRenamedFile = repo.Lookup<Commit>("4be51d6").Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, commitTreeWithRenamedFile))
                {
                    Assert.Single(changes);
                    Assert.Equal("my-name-does-not-feel-right.txt", changes.Single(c => c.Path == "super-file.txt").OldPath);
                    Assert.Single(changes.Renamed);
                }
            }
        }

        [Fact]
        public void DetectsTheExactRenamingOfFilesByDefault()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string renamedPath = "renamed.txt";

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                Commands.Move(repo, originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Renamed);
                    Assert.Equal(originalPath, changes.Renamed.Single().OldPath);
                    Assert.Equal(renamedPath, changes.Renamed.Single().Path);
                }
            }
        }

        [Fact(Skip = "Not supported by libgit2 as yet")]
        public void RenameDetectionObeysConfigurationSetting()
        {
            // TODO: set the repo's diff.renames setting, and pass a structure that adjusts thresholds
        }

        [Fact]
        public void RenameThresholdsAreObeyed()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string renamedPath = "renamed.txt";

                // 4 lines
                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");
                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                // 8 lines, 50% are from original file
                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\ne\nf\ng\nh\n");
                Commands.Stage(repo, originalPath);
                Commands.Move(repo, originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                var compareOptions = new CompareOptions
                {
                    Similarity = new SimilarityOptions
                    {
                        RenameDetectionMode = RenameDetectionMode.Renames,
                    },
                };

                compareOptions.Similarity.RenameThreshold = 30;

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree, compareOptions: compareOptions))
                    Assert.True(changes.All(x => x.Status == ChangeKind.Renamed));

                compareOptions.Similarity.RenameThreshold = 90;

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree, compareOptions: compareOptions))
                    Assert.DoesNotContain(changes, x => x.Status == ChangeKind.Renamed);
            }
        }

        [Fact]
        public void ExactModeDetectsExactRenames()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string renamedPath = "renamed.txt";

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                Commands.Move(repo, originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions
                    {
                        Similarity = SimilarityOptions.Exact,
                    }))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Renamed);
                    Assert.Equal(originalPath, changes.Renamed.Single().OldPath);
                    Assert.Equal(renamedPath, changes.Renamed.Single().Path);
                }
            }
        }

        [Fact]
        public void ExactModeDetectsExactCopies()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string copiedPath = "copied.txt";
                var originalFullPath = Path.Combine(repo.Info.WorkingDirectory, originalPath);
                var copiedFullPath = Path.Combine(repo.Info.WorkingDirectory, copiedPath);

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");
                Commands.Stage(repo, originalPath);
                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                Commands.Stage(repo, copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions
                    {
                        Similarity = SimilarityOptions.Exact,
                    }))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Copied);
                }
            }
        }

        [Fact]
        public void ExactModeDoesntDetectRenamesWithEdits()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string renamedPath = "renamed.txt";

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                Commands.Move(repo, originalPath, renamedPath);
                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, renamedPath), "e\nf\n");
                Commands.Stage(repo, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions
                    {
                        Similarity = SimilarityOptions.Exact,
                    }))
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Empty(changes.Renamed);
                    Assert.Single(changes.Added);
                    Assert.Single(changes.Deleted);
                }
            }
        }

        [Fact]
        public void CanIncludeUnmodifiedEntriesWhenDetectingTheExactRenamingOfFilesWhenEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string copiedPath = "copied.txt";
                string originalFullPath = Path.Combine(repo.Info.WorkingDirectory, originalPath);
                string copiedFullPath = Path.Combine(repo.Info.WorkingDirectory, copiedPath);

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                Commands.Stage(repo, copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.CopiesHarder,
                            IncludeUnmodified = true,
                        }))
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Single(changes.Unmodified);
                    Assert.Single(changes.Copied);
                    Assert.Equal(originalPath, changes.Copied.Single().OldPath);
                    Assert.Equal(copiedPath, changes.Copied.Single().Path);
                }
            }
        }

        [Fact]
        public void CanNotDetectTheExactRenamingFilesWhenNotEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string renamedPath = "renamed.txt";

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                Commands.Move(repo, originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.None,
                        }))
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Empty(changes.Renamed);
                }
            }
        }

        [Fact]
        public void CanDetectTheExactCopyingOfNonModifiedFilesWhenEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string copiedPath = "copied.txt";
                string originalFullPath = Path.Combine(repo.Info.WorkingDirectory, originalPath);
                string copiedFullPath = Path.Combine(repo.Info.WorkingDirectory, copiedPath);

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                Commands.Stage(repo, copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.CopiesHarder,
                        }))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Copied);
                    Assert.Equal(originalPath, changes.Copied.Single().OldPath);
                    Assert.Equal(copiedPath, changes.Copied.Single().Path);
                }
            }
        }

        [Fact]
        public void CanNotDetectTheExactCopyingOfNonModifiedFilesWhenNotEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string copiedPath = "copied.txt";
                string originalFullPath = Path.Combine(repo.Info.WorkingDirectory, originalPath);
                string copiedFullPath = Path.Combine(repo.Info.WorkingDirectory, copiedPath);

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                Commands.Stage(repo, copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree))
                {
                    Assert.Single(changes);
                    Assert.Empty(changes.Copied);
                }
            }
        }

        [Fact]
        public void CanDetectTheExactCopyingOfModifiedFilesWhenEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string copiedPath = "copied.txt";
                string originalFullPath = Path.Combine(repo.Info.WorkingDirectory, originalPath);
                string copiedFullPath = Path.Combine(repo.Info.WorkingDirectory, copiedPath);

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                Touch(repo.Info.WorkingDirectory, originalPath, "e\n");

                Commands.Stage(repo, originalPath);
                Commands.Stage(repo, copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.Copies,
                        }))
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Single(changes.Copied);
                    Assert.Equal(originalPath, changes.Copied.Single().OldPath);
                    Assert.Equal(copiedPath, changes.Copied.Single().Path);
                }
            }
        }

        [Fact]
        public void CanNotDetectTheExactCopyingOfModifiedFilesWhenNotEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string copiedPath = "copied.txt";
                string originalFullPath = Path.Combine(repo.Info.WorkingDirectory, originalPath);
                string copiedFullPath = Path.Combine(repo.Info.WorkingDirectory, copiedPath);

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");

                Commands.Stage(repo, originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                File.AppendAllText(originalFullPath, "e\n");

                Commands.Stage(repo, originalPath);
                Commands.Stage(repo, copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree))
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Empty(changes.Copied);
                }
            }
        }

        [Fact]
        public void CanIncludeUnmodifiedEntriesWhenEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "a.txt", "abc\ndef\n");
                Touch(repo.Info.WorkingDirectory, "b.txt", "abc\ndef\n");

                Commands.Stage(repo, new[] { "a.txt", "b.txt" });
                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "b.txt"), "ghi\njkl\n");
                Commands.Stage(repo, "b.txt");
                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions { IncludeUnmodified = true }))
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Single(changes.Unmodified);
                    Assert.Single(changes.Modified);
                }
            }
        }

        [Fact]
        public void CanDetectTheExactRenamingExactCopyingOfNonModifiedAndModifiedFilesWhenEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath = "original.txt";
                const string renamedPath = "renamed.txt";
                const string originalPath2 = "original2.txt";
                const string copiedPath1 = "copied.txt";
                const string originalPath3 = "original3.txt";
                const string copiedPath2 = "copied2.txt";

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");
                Touch(repo.Info.WorkingDirectory, originalPath2, "1\n2\n3\n4\n");
                Touch(repo.Info.WorkingDirectory, originalPath3, "5\n6\n7\n8\n");

                Commands.Stage(repo, originalPath);
                Commands.Stage(repo, originalPath2);
                Commands.Stage(repo, originalPath3);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                var originalFullPath2 = Path.Combine(repo.Info.WorkingDirectory, originalPath2);
                var originalFullPath3 = Path.Combine(repo.Info.WorkingDirectory, originalPath3);
                var copiedFullPath1 = Path.Combine(repo.Info.WorkingDirectory, copiedPath1);
                var copiedFullPath2 = Path.Combine(repo.Info.WorkingDirectory, copiedPath2);
                File.Copy(originalFullPath2, copiedFullPath1);
                File.Copy(originalFullPath3, copiedFullPath2);
                File.AppendAllText(originalFullPath3, "9\n");

                Commands.Stage(repo, originalPath3);
                Commands.Stage(repo, copiedPath1);
                Commands.Stage(repo, copiedPath2);
                Commands.Move(repo, originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                using (var changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.CopiesHarder,
                        }))
                {
                    Assert.Equal(4, changes.Count());
                    Assert.Single(changes.Modified);
                    Assert.Single(changes.Renamed);
                    Assert.Equal(originalPath, changes.Renamed.Single().OldPath);
                    Assert.Equal(renamedPath, changes.Renamed.Single().Path);
                    Assert.Equal(2, changes.Copied.Count());
                    Assert.Equal(originalPath2, changes.Copied.ElementAt(0).OldPath);
                    Assert.Equal(copiedPath1, changes.Copied.ElementAt(0).Path);
                    Assert.Equal(originalPath3, changes.Copied.ElementAt(1).OldPath);
                    Assert.Equal(copiedPath2, changes.Copied.ElementAt(1).Path);
                }
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
        [Theory]
        [InlineData(0, 175)]
        [InlineData(1, 191)]
        [InlineData(2, 184)]
        [InlineData(3, 187)]
        [InlineData(4, 193)]
        public void CanCompareTwoVersionsOfAFileWithATrailingNewlineDeletion(int contextLines, int expectedPatchLength)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree commitTreeWithUpdatedFile = repo.Lookup<Commit>("ec9e401").Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, commitTreeWithUpdatedFile))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Modified);
                }

                using (var patch = repo.Diff.Compare<Patch>(rootCommitTree, commitTreeWithUpdatedFile,
                                        compareOptions: new CompareOptions { ContextLines = contextLines }))
                {
                    Assert.Equal(expectedPatchLength, patch.Content.Length);

                    PatchEntryChanges entryChanges = patch["numbers.txt"];

                    Assert.Equal(2, entryChanges.LinesAdded);
                    Assert.Equal(1, entryChanges.LinesDeleted);
                    Assert.Equal(expectedPatchLength, entryChanges.Patch.Length);
                    Assert.Equal("numbers.txt", entryChanges.Path);
                }
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
        [Theory]
        [InlineData(0, 3)]
        [InlineData(0, 4)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 4)]
        [InlineData(2, 5)]
        [InlineData(3, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 0)]
        [InlineData(4, 1)]
        public void CanCompareTwoVersionsOfAFileWithADiffOfTwoHunks(int contextLines, int interhunkLines)
        {
            var compareOptions = new CompareOptions
            {
                ContextLines = contextLines,
                InterhunkLines = interhunkLines,
                Similarity = SimilarityOptions.None,
            };

            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree mergedCommitTree = repo.Lookup<Commit>("7252fe2").Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, mergedCommitTree, compareOptions: compareOptions))
                {
                    Assert.Equal(3, changes.Count());
                    Assert.Single(changes.Modified);
                    Assert.Single(changes.Deleted);
                    Assert.Single(changes.Added);

                    Assert.Equal(Mode.Nonexistent, changes.Single(c => c.Path == "my-name-does-not-feel-right.txt").Mode);
                }

                using (var patch = repo.Diff.Compare<Patch>(rootCommitTree, mergedCommitTree, compareOptions: compareOptions))
                {
                    PatchEntryChanges entryChanges = patch["numbers.txt"];

                    Assert.Equal(3, entryChanges.LinesAdded);
                    Assert.Equal(1, entryChanges.LinesDeleted);
                    Assert.Equal(Expected("f8d44d7...7252fe2/numbers.txt-{0}-{1}.diff", contextLines, interhunkLines),
                        entryChanges.Patch);
                    Assert.Equal(Expected("f8d44d7...7252fe2/full-{0}-{1}.diff", contextLines, interhunkLines),
                        patch);
                    Assert.Equal("numbers.txt", entryChanges.Path);
                }
            }
        }

        private void CanHandleTwoTreeEntryChangesWithTheSamePath(SimilarityOptions similarity, Action<string, TreeChanges> verifier)
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Blob mainContent = OdbHelper.CreateBlob(repo, "awesome content\n" + new string('b', 4096));
                Blob linkContent = OdbHelper.CreateBlob(repo, "../../objc/Nu.h");

                string path = Path.Combine("include", "Nu", "Nu.h");

                var tdOld = new TreeDefinition()
                    .Add(path, linkContent, Mode.SymbolicLink)
                    .Add("objc/Nu.h", mainContent, Mode.NonExecutableFile);

                Tree treeOld = repo.ObjectDatabase.CreateTree(tdOld);

                var tdNew = new TreeDefinition()
                    .Add(path, mainContent, Mode.NonExecutableFile);

                Tree treeNew = repo.ObjectDatabase.CreateTree(tdNew);

                using (var changes = repo.Diff.Compare<TreeChanges>(treeOld, treeNew,
                    compareOptions: new CompareOptions
                    {
                        Similarity = similarity,
                    }))
                {
                    verifier(path, changes);
                }
            }
        }

        [Fact]
        public void CanHandleTwoTreeEntryChangesWithTheSamePathUsingSimilarityNone()
        {
            // $ git diff-tree --name-status --no-renames -r 2ccccf8 e829333
            // T       include/Nu/Nu.h
            // D       objc/Nu.h

            CanHandleTwoTreeEntryChangesWithTheSamePath(SimilarityOptions.None,
                (path, changes) =>
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Single(changes.Deleted);
                    Assert.Single(changes.TypeChanged);

                    TreeEntryChanges change = changes.Single(c => c.Path == path);
                    Assert.Equal(Mode.SymbolicLink, change.OldMode);
                    Assert.Equal(Mode.NonExecutableFile, change.Mode);
                    Assert.Equal(ChangeKind.TypeChanged, change.Status);
                    Assert.Equal(path, change.Path);
                });
        }

        [Fact]
        public void CanHandleTwoTreeEntryChangesWithTheSamePathUsingSimilarityDefault()
        {
            // $ git diff-tree --name-status --find-renames -r 2ccccf8 e829333
            // T       include/Nu/Nu.h
            // D       objc/Nu.h

            CanHandleTwoTreeEntryChangesWithTheSamePath(SimilarityOptions.Default,
                (path, changes) =>
                {
                    Assert.Equal(2, changes.Count());
                    Assert.Single(changes.Deleted);
                    Assert.Single(changes.Renamed);

                    TreeEntryChanges renamed = changes.Renamed.Single();
                    Assert.Equal(Mode.NonExecutableFile, renamed.OldMode);
                    Assert.Equal(Mode.NonExecutableFile, renamed.Mode);
                    Assert.Equal(ChangeKind.Renamed, renamed.Status);
                    Assert.Equal(path, renamed.Path);

                    TreeEntryChanges deleted = changes.Deleted.Single();
                    Assert.Equal(Mode.SymbolicLink, deleted.OldMode);
                    Assert.Equal(Mode.Nonexistent, deleted.Mode);
                    Assert.Equal(ChangeKind.Deleted, deleted.Status);
                    Assert.Equal(path, deleted.Path);
                });
        }

        [Fact]
        public void CanCompareATreeAgainstANullTree()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Tree tree = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                using (var changes = repo.Diff.Compare<TreeChanges>(tree, null))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Deleted);

                    Assert.Equal("readme.txt", changes.Deleted.Single().Path);
                }

                using (var changes = repo.Diff.Compare<TreeChanges>(null, tree))
                {
                    Assert.Single(changes);
                    Assert.Single(changes.Added);

                    Assert.Equal("readme.txt", changes.Added.Single().Path);
                }
            }
        }

        [Fact]
        public void ComparingTwoNullTreesReturnsAnEmptyTreeChanges()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                using (var changes = repo.Diff.Compare<TreeChanges>(default(Tree), default(Tree)))
                {
                    Assert.Empty(changes);
                }
            }
        }

        [Fact]
        public void ComparingReliesOnProvidedConfigEntriesIfAny()
        {
            const string file = "1/branch_file.txt";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                TreeEntry entry = repo.Head[file];
                Assert.Equal(Mode.ExecutableFile, entry.Mode);

                // Recreate the file in the workdir without the executable bit
                string fullpath = Path.Combine(repo.Info.WorkingDirectory, file);
                File.Delete(fullpath);
                using (var stream = ((Blob)(entry.Target)).GetContentStream())
                {
                    Touch(repo.Info.WorkingDirectory, file, stream);
                }

                // Unset the local core.filemode, if any.
                repo.Config.Unset("core.filemode");
            }

            using (var repo = new Repository(path))
            {
                SetFilemode(repo, true);
                using (var changes = repo.Diff.Compare<TreeChanges>(new[] { file }))
                {
                    Assert.Single(changes);

                    var change = changes.Modified.Single();
                    Assert.Equal(Mode.ExecutableFile, change.OldMode);
                    Assert.Equal(Mode.NonExecutableFile, change.Mode);
                }
            }

            using (var repo = new Repository(path))
            {
                SetFilemode(repo, false);
                using (var changes = repo.Diff.Compare<TreeChanges>(new[] { file }))
                {
                    Assert.Empty(changes);
                }
            }
        }

        void SetFilemode(Repository repo, bool value)
        {
            repo.Config.Set("core.filemode", value);
        }

        [Fact]
        public void RetrievingDiffChangesMustAlwaysBeCaseSensitive()
        {
            ObjectId treeOldOid, treeNewOid;

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Blob oldContent = OdbHelper.CreateBlob(repo, "awesome content\n");
                Blob newContent = OdbHelper.CreateBlob(repo, "more awesome content\n");

                var td = new TreeDefinition()
                    .Add("A.TXT", oldContent, Mode.NonExecutableFile)
                    .Add("a.txt", oldContent, Mode.NonExecutableFile);

                treeOldOid = repo.ObjectDatabase.CreateTree(td).Id;

                td = new TreeDefinition()
                    .Add("A.TXT", newContent, Mode.NonExecutableFile)
                    .Add("a.txt", newContent, Mode.NonExecutableFile);

                treeNewOid = repo.ObjectDatabase.CreateTree(td).Id;
            }

            using (var repo = new Repository(repoPath))
            {
                using (var changes = repo.Diff.Compare<TreeChanges>(repo.Lookup<Tree>(treeOldOid), repo.Lookup<Tree>(treeNewOid)))
                {
                    Assert.Equal(ChangeKind.Modified, changes.Single(c => c.Path == "a.txt").Status);
                    Assert.Equal(ChangeKind.Modified, changes.Single(c => c.Path == "A.TXT").Status);
                }
            }
        }

        [Fact]
        public void RetrievingDiffContainsRightAmountOfAddedAndDeletedLines()
        {
            ObjectId treeOldOid, treeNewOid;

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Blob oldContent = OdbHelper.CreateBlob(repo, "awesome content\n");
                Blob newContent = OdbHelper.CreateBlob(repo, "more awesome content\n");

                var td = new TreeDefinition()
                    .Add("A.TXT", oldContent, Mode.NonExecutableFile)
                    .Add("a.txt", oldContent, Mode.NonExecutableFile);

                treeOldOid = repo.ObjectDatabase.CreateTree(td).Id;

                td = new TreeDefinition()
                    .Add("A.TXT", newContent, Mode.NonExecutableFile)
                    .Add("a.txt", newContent, Mode.NonExecutableFile);

                treeNewOid = repo.ObjectDatabase.CreateTree(td).Id;
            }

            using (var repo = new Repository(repoPath))
            {
                using (var changes = repo.Diff.Compare<Patch>(repo.Lookup<Tree>(treeOldOid), repo.Lookup<Tree>(treeNewOid)))
                {
                    foreach (var entry in changes)
                    {
                        Assert.Single(entry.AddedLines);
                        Assert.Single(entry.DeletedLines);
                    }
                }
            }
        }

        [Fact]
        public void UsingPatienceAlgorithmCompareOptionProducesPatienceDiff()
        {
            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                Func<string, Tree> fromString =
                    s =>
                        repo.ObjectDatabase.CreateTree(new TreeDefinition().Add("file.txt",
                            OdbHelper.CreateBlob(repo, s), Mode.NonExecutableFile));

                Tree treeOld = fromString(new StringBuilder()
                    .Append("aaaaaa\n")
                    .Append("aaaaaa\n")
                    .Append("bbbbbb\n")
                    .Append("bbbbbb\n")
                    .Append("cccccc\n")
                    .Append("cccccc\n")
                    .Append("abc\n").ToString());

                Tree treeNew = fromString(new StringBuilder()
                    .Append("abc\n")
                    .Append("aaaaaa\n")
                    .Append("aaaaaa\n")
                    .Append("bbbbbb\n")
                    .Append("bbbbbb\n")
                    .Append("cccccc\n")
                    .Append("cccccc\n").ToString());

                string diffDefault = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index 3299d68..accc3bd 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1,7 +1,7 @@\n")
                    .Append("+abc\n")
                    .Append(" aaaaaa\n")
                    .Append(" aaaaaa\n")
                    .Append(" bbbbbb\n")
                    .Append(" bbbbbb\n")
                    .Append(" cccccc\n")
                    .Append(" cccccc\n")
                    .Append("-abc\n").ToString();

                string diffPatience = new StringBuilder()
                    .Append("diff --git a/file.txt b/file.txt\n")
                    .Append("index 3299d68..accc3bd 100644\n")
                    .Append("--- a/file.txt\n")
                    .Append("+++ b/file.txt\n")
                    .Append("@@ -1,7 +1,7 @@\n")
                    .Append("-aaaaaa\n")
                    .Append("-aaaaaa\n")
                    .Append("-bbbbbb\n")
                    .Append("-bbbbbb\n")
                    .Append("-cccccc\n")
                    .Append("-cccccc\n")
                    .Append(" abc\n")
                    .Append("+aaaaaa\n")
                    .Append("+aaaaaa\n")
                    .Append("+bbbbbb\n")
                    .Append("+bbbbbb\n")
                    .Append("+cccccc\n")
                    .Append("+cccccc\n").ToString();

                using (var changes = repo.Diff.Compare<Patch>(treeOld, treeNew))
                    Assert.Equal(diffDefault, changes);

                using (var changes = repo.Diff.Compare<Patch>(treeOld, treeNew,
                    compareOptions: new CompareOptions { Algorithm = DiffAlgorithm.Patience }))
                    Assert.Equal(diffPatience, changes);
            }
        }

        [Fact]
        public void DiffThrowsANotFoundExceptionIfATreeIsMissing()
        {
            string repoPath = SandboxBareTestRepo();

            // Manually delete the tree object to simulate a partial clone
            File.Delete(Path.Combine(repoPath, "objects", "58", "1f9824ecaf824221bd36edf5430f2739a7c4f5")); 

            using (var repo = new Repository(repoPath))
            {
                // The commit is there but its tree is missing
                var commit = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                Assert.NotNull(commit);
                Assert.Equal("581f9824ecaf824221bd36edf5430f2739a7c4f5", commit.Tree.Sha);
                Assert.True(commit.Tree.IsMissing);

                var tree = repo.Lookup<Tree>("581f9824ecaf824221bd36edf5430f2739a7c4f5");
                Assert.Null(tree);

                var otherCommit = repo.Lookup<Commit>("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                Assert.NotNull(otherCommit);
                Assert.False(otherCommit.Tree.IsMissing);

                Assert.Throws<NotFoundException>(() =>
                {
                    using (repo.Diff.Compare<TreeChanges>(commit.Tree, otherCommit.Tree)) {}
                });

                Assert.Throws<NotFoundException>(() =>
                {
                    using (repo.Diff.Compare<TreeChanges>(otherCommit.Tree, commit.Tree)) {}
                });
            }
        }
    }
}
