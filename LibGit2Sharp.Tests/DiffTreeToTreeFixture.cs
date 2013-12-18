using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

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

                var changes = repo.Diff.Compare<TreeChanges>(tree, tree);
                var patch = repo.Diff.Compare<Patch>(tree, tree);

                Assert.Empty(changes);
                Assert.Empty(patch);
                Assert.Equal(String.Empty, patch);
            }
        }

        [Fact]
        public void RetrievingANonExistentFileChangeReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;

                var changes = repo.Diff.Compare<TreeChanges>(tree, tree);

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

                var changes = repo.Diff.Compare<TreeChanges>(parentCommitTree, commitTree);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Added.Count());

                TreeEntryChanges treeEntryChanges = changes["1.txt"];

                var patch = repo.Diff.Compare<Patch>(parentCommitTree, commitTree);
                Assert.False(patch["1.txt"].IsBinaryComparison);

                Assert.Equal("1.txt", treeEntryChanges.Path);
                Assert.Equal(ChangeKind.Added, treeEntryChanges.Status);

                Assert.Equal(treeEntryChanges, changes.Added.Single());

                Assert.Equal(Mode.Nonexistent, treeEntryChanges.OldMode);
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
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                const string filename = "binfile.foo";
                var filepath = Path.Combine(repo.Info.WorkingDirectory, filename);

                CreateBinaryFile(filepath);

                repo.Index.Stage(filename);
                var commit = repo.Commit("Add binary file", Constants.Signature, Constants.Signature);

                File.AppendAllText(filepath, "abcdef");

                var patch = repo.Diff.Compare<Patch>(commit.Tree, DiffTargets.WorkingDirectory, new[] { filename });
                Assert.True(patch[filename].IsBinaryComparison);

                repo.Index.Stage(filename);
                var commit2 = repo.Commit("Update binary file", Constants.Signature, Constants.Signature);

                var patch2 = repo.Diff.Compare<Patch>(commit.Tree, commit2.Tree, new[] { filename });
                Assert.True(patch2[filename].IsBinaryComparison);
            }
        }

        [Fact]
        public void CanDetectABinaryDeletion()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                const string filename = "binfile.foo";
                var filepath = Path.Combine(repo.Info.WorkingDirectory, filename);

                CreateBinaryFile(filepath);

                repo.Index.Stage(filename);
                var commit = repo.Commit("Add binary file", Constants.Signature, Constants.Signature);

                File.Delete(filepath);

                var patch = repo.Diff.Compare<Patch>(commit.Tree, DiffTargets.WorkingDirectory, new [] {filename});
                Assert.True(patch[filename].IsBinaryComparison);

                repo.Index.Remove(filename);
                var commit2 = repo.Commit("Delete binary file", Constants.Signature, Constants.Signature);

                var patch2 = repo.Diff.Compare<Patch>(commit.Tree, commit2.Tree, new[] { filename });
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
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Head.Tip.Tree;
                Tree ancestor = repo.Lookup<Commit>("9fd738e").Tree;

                var changes = repo.Diff.Compare<TreeChanges>(ancestor, tree, new[] { "1" });
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

                var changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree);

                Assert.Equal(10, changes.Count());
                Assert.Equal(9, changes.Added.Count());
                Assert.Equal(1, changes.Deleted.Count());

                Assert.Equal("readme.txt", changes.Deleted.Single().Path);
                Assert.Equal(new[] { "1.txt", subBranchFilePath, "README", "branch_file.txt", "deleted_staged_file.txt", "deleted_unstaged_file.txt", "modified_staged_file.txt", "modified_unstaged_file.txt", "new.txt" },
                             changes.Added.Select(x => x.Path).OrderBy(p => p, StringComparer.Ordinal).ToArray());

                var patch = repo.Diff.Compare<Patch>(commitTreeWithDifferentAncestor, commitTree);
                Assert.Equal(9, patch.LinesAdded);
                Assert.Equal(2, patch.LinesDeleted);
                Assert.Equal(2, patch["readme.txt"].LinesDeleted);
            }
        }

        [Fact]
        public void CanCompareATreeAgainstAnotherTreeWithLaxExplicitPathsValidationAndNonExistentPath()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree commitTreeWithDifferentAncestor = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                var changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                        new[] { "if-I-exist-this-test-is-really-unlucky.txt" }, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false });
                Assert.Equal(0, changes.Count());

                changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                    new[] { "if-I-exist-this-test-is-really-unlucky.txt" });
                Assert.Equal(0, changes.Count());
            }
        }

        [Fact]
        public void ComparingATreeAgainstAnotherTreeWithStrictExplicitPathsValidationThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
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
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree commitTreeWithRenamedFile = repo.Lookup<Commit>("4be51d6").Tree;

                var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, commitTreeWithRenamedFile);

                Assert.Equal(1, changes.Count());
                Assert.Equal("super-file.txt", changes["super-file.txt"].Path);
                Assert.Equal("my-name-does-not-feel-right.txt", changes["super-file.txt"].OldPath);
                Assert.Equal(1, changes.Renamed.Count());
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                repo.Index.Move(originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Renamed.Count());
                Assert.Equal("original.txt", changes.Renamed.Single().OldPath);
                Assert.Equal("renamed.txt", changes.Renamed.Single().Path);
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
                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                // 8 lines, 50% are from original file
                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\ne\nf\ng\nh\n");
                repo.Index.Stage(originalPath);
                repo.Index.Move(originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                var compareOptions = new CompareOptions
                {
                    Similarity = new SimilarityOptions
                    {
                        RenameDetectionMode = RenameDetectionMode.Renames,
                    },
                };

                compareOptions.Similarity.RenameThreshold = 30;
                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree, compareOptions: compareOptions);
                Assert.True(changes.All(x => x.Status == ChangeKind.Renamed));

                compareOptions.Similarity.RenameThreshold = 90;
                changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree, compareOptions: compareOptions);
                Assert.False(changes.Any(x => x.Status == ChangeKind.Renamed));
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                repo.Index.Move(originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions
                    {
                        Similarity = SimilarityOptions.Exact,
                    });

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Renamed.Count());
                Assert.Equal("original.txt", changes.Renamed.Single().OldPath);
                Assert.Equal("renamed.txt", changes.Renamed.Single().Path);
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
                repo.Index.Stage(originalPath);
                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                repo.Index.Stage(copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions
                    {
                        Similarity = SimilarityOptions.Exact,
                    });

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Copied.Count());
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                repo.Index.Move(originalPath, renamedPath);
                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, renamedPath), "e\nf\n");
                repo.Index.Stage(renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions
                    {
                        Similarity = SimilarityOptions.Exact,
                    });

                Assert.Equal(2, changes.Count());
                Assert.Equal(0, changes.Renamed.Count());
                Assert.Equal(1, changes.Added.Count());
                Assert.Equal(1, changes.Deleted.Count());
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                repo.Index.Stage(copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.CopiesHarder,
                            IncludeUnmodified = true,
                        });

                Assert.Equal(2, changes.Count());
                Assert.Equal(1, changes.Unmodified.Count());
                Assert.Equal(1, changes.Copied.Count());
                Assert.Equal("original.txt", changes.Copied.Single().OldPath);
                Assert.Equal("copied.txt", changes.Copied.Single().Path);
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                repo.Index.Move(originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.None,
                        });

                Assert.Equal(2, changes.Count());
                Assert.Equal(0, changes.Renamed.Count());
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                repo.Index.Stage(copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.CopiesHarder,
                        });

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Copied.Count());
                Assert.Equal("original.txt", changes.Copied.Single().OldPath);
                Assert.Equal("copied.txt", changes.Copied.Single().Path);
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                repo.Index.Stage(copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree);

                Assert.Equal(1, changes.Count());
                Assert.Equal(0, changes.Copied.Count());
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                Touch(repo.Info.WorkingDirectory, originalPath, "e\n");

                repo.Index.Stage(originalPath);
                repo.Index.Stage(copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.Copies,
                        });

                Assert.Equal(2, changes.Count());
                Assert.Equal(1, changes.Copied.Count());
                Assert.Equal("original.txt", changes.Copied.Single().OldPath);
                Assert.Equal("copied.txt", changes.Copied.Single().Path);
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

                repo.Index.Stage(originalPath);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.Copy(originalFullPath, copiedFullPath);
                File.AppendAllText(originalFullPath, "e\n");

                repo.Index.Stage(originalPath);
                repo.Index.Stage(copiedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree);

                Assert.Equal(2, changes.Count());
                Assert.Equal(0, changes.Copied.Count());
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

                repo.Index.Stage(new[] {"a.txt", "b.txt"});
                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "b.txt"), "ghi\njkl\n");
                repo.Index.Stage("b.txt");
                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions: new CompareOptions {IncludeUnmodified = true});

                Assert.Equal(2, changes.Count());
                Assert.Equal(1, changes.Unmodified.Count());
                Assert.Equal(1, changes.Modified.Count());
            }
        }

        [Fact]
        public void CanDetectTheExactRenamingExactCopyingOfNonModifiedAndModifiedFilesWhenEnabled()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            var path = Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(path))
            {
                const string originalPath =  "original.txt";
                const string renamedPath =   "renamed.txt";
                const string originalPath2 = "original2.txt";
                const string copiedPath1 =   "copied.txt";
                const string originalPath3 = "original3.txt";
                const string copiedPath2 =   "copied2.txt";

                Touch(repo.Info.WorkingDirectory, originalPath, "a\nb\nc\nd\n");
                Touch(repo.Info.WorkingDirectory, originalPath2, "1\n2\n3\n4\n");
                Touch(repo.Info.WorkingDirectory, originalPath3, "5\n6\n7\n8\n");

                repo.Index.Stage(originalPath);
                repo.Index.Stage(originalPath2);
                repo.Index.Stage(originalPath3);

                Commit old = repo.Commit("Initial", Constants.Signature, Constants.Signature);

                var originalFullPath2 = Path.Combine(repo.Info.WorkingDirectory, originalPath2);
                var originalFullPath3 = Path.Combine(repo.Info.WorkingDirectory, originalPath3);
                var copiedFullPath1 = Path.Combine(repo.Info.WorkingDirectory, copiedPath1);
                var copiedFullPath2 = Path.Combine(repo.Info.WorkingDirectory, copiedPath2);
                File.Copy(originalFullPath2, copiedFullPath1);
                File.Copy(originalFullPath3, copiedFullPath2);
                File.AppendAllText(originalFullPath3, "9\n");

                repo.Index.Stage(originalPath3);
                repo.Index.Stage(copiedPath1);
                repo.Index.Stage(copiedPath2);
                repo.Index.Move(originalPath, renamedPath);

                Commit @new = repo.Commit("Updated", Constants.Signature, Constants.Signature);

                TreeChanges changes = repo.Diff.Compare<TreeChanges>(old.Tree, @new.Tree,
                    compareOptions:
                        new CompareOptions
                        {
                            Similarity = SimilarityOptions.CopiesHarder,
                        });

                Assert.Equal(4, changes.Count());
                Assert.Equal(1, changes.Modified.Count());
                Assert.Equal(1, changes.Renamed.Count());
                Assert.Equal("original.txt", changes.Renamed.Single().OldPath);
                Assert.Equal("renamed.txt", changes.Renamed.Single().Path);
                Assert.Equal(2, changes.Copied.Count());
                Assert.Equal("original2.txt", changes.Copied.ElementAt(0).OldPath);
                Assert.Equal("copied.txt", changes.Copied.ElementAt(0).Path);
                Assert.Equal("original3.txt", changes.Copied.ElementAt(1).OldPath);
                Assert.Equal("copied2.txt", changes.Copied.ElementAt(1).Path);
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
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree commitTreeWithUpdatedFile = repo.Lookup<Commit>("ec9e401").Tree;

                var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, commitTreeWithUpdatedFile);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Modified.Count());

                var patch = repo.Diff.Compare<Patch>(rootCommitTree, commitTreeWithUpdatedFile,
                                        compareOptions: new CompareOptions { ContextLines = contextLines });

                Assert.Equal(expectedPatchLength, patch.Content.Length);

                ContentChanges contentChanges = patch["numbers.txt"];

                Assert.Equal(2, contentChanges.LinesAdded);
                Assert.Equal(1, contentChanges.LinesDeleted);
                Assert.Equal(expectedPatchLength, contentChanges.Patch.Length);
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

            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree mergedCommitTree = repo.Lookup<Commit>("7252fe2").Tree;

                var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, mergedCommitTree, compareOptions: compareOptions);

                Assert.Equal(3, changes.Count());
                Assert.Equal(1, changes.Modified.Count());
                Assert.Equal(1, changes.Deleted.Count());
                Assert.Equal(1, changes.Added.Count());

                Assert.Equal(Mode.Nonexistent, changes["my-name-does-not-feel-right.txt"].Mode);

                var patch = repo.Diff.Compare<Patch>(rootCommitTree, mergedCommitTree, compareOptions: compareOptions);

                ContentChanges contentChanges = patch["numbers.txt"];

                Assert.Equal(3, contentChanges.LinesAdded);
                Assert.Equal(1, contentChanges.LinesDeleted);
                Assert.Equal(Expected("f8d44d7...7252fe2/numbers.txt-{0}-{1}.diff", contextLines, interhunkLines),
                    contentChanges.Patch);
                Assert.Equal(Expected("f8d44d7...7252fe2/full-{0}-{1}.diff", contextLines, interhunkLines),
                    patch);
            }
        }

        [Fact]
        public void CanHandleTwoTreeEntryChangesWithTheSamePath()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Blob mainContent = OdbHelper.CreateBlob(repo, "awesome content\n");
                Blob linkContent = OdbHelper.CreateBlob(repo, "../../objc/Nu.h");

                string path = string.Format("include{0}Nu{0}Nu.h", Path.DirectorySeparatorChar);

                var tdOld = new TreeDefinition()
                    .Add(path, linkContent, Mode.SymbolicLink)
                    .Add("objc/Nu.h", mainContent, Mode.NonExecutableFile);

                Tree treeOld = repo.ObjectDatabase.CreateTree(tdOld);

                var tdNew = new TreeDefinition()
                    .Add(path, mainContent, Mode.NonExecutableFile);

                Tree treeNew = repo.ObjectDatabase.CreateTree(tdNew);

                var changes = repo.Diff.Compare<TreeChanges>(treeOld, treeNew,
                    compareOptions: new CompareOptions
                    {
                        Similarity = SimilarityOptions.None,
                    });

                /*
                 * $ git diff-tree -p 5c87b67 d5278d0
                 * diff --git a/include/Nu/Nu.h b/include/Nu/Nu.h
                 * deleted file mode 120000
                 * index 19bf568..0000000
                 * --- a/include/Nu/Nu.h
                 * +++ /dev/null
                 * @@ -1 +0,0 @@
                 * -../../objc/Nu.h
                 * \ No newline at end of file
                 * diff --git a/include/Nu/Nu.h b/include/Nu/Nu.h
                 * new file mode 100644
                 * index 0000000..f9e6561
                 * --- /dev/null
                 * +++ b/include/Nu/Nu.h
                 * @@ -0,0 +1 @@
                 * +awesome content
                 * diff --git a/objc/Nu.h b/objc/Nu.h
                 * deleted file mode 100644
                 * index f9e6561..0000000
                 * --- a/objc/Nu.h
                 * +++ /dev/null
                 * @@ -1 +0,0 @@
                 * -awesome content
                 */

                Assert.Equal(1, changes.Deleted.Count());
                Assert.Equal(0, changes.Modified.Count());
                Assert.Equal(1, changes.TypeChanged.Count());

                TreeEntryChanges change = changes[path];
                Assert.Equal(Mode.SymbolicLink, change.OldMode);
                Assert.Equal(Mode.NonExecutableFile, change.Mode);
                Assert.Equal(ChangeKind.TypeChanged, change.Status);
                Assert.Equal(path, change.Path);
            }
        }

        [Fact]
        public void CanCompareATreeAgainstANullTree()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree tree = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                var changes = repo.Diff.Compare<TreeChanges>(tree, null);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Deleted.Count());

                Assert.Equal("readme.txt", changes.Deleted.Single().Path);

                changes = repo.Diff.Compare<TreeChanges>(null, tree);

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Added.Count());

                Assert.Equal("readme.txt", changes.Added.Single().Path);
            }
        }

        [Fact]
        public void ComparingTwoNullTreesReturnsAnEmptyTreeChanges()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var changes = repo.Diff.Compare<TreeChanges>(default(Tree), default(Tree));

                Assert.Equal(0, changes.Count());
            }
        }

        [Fact]
        public void ComparingReliesOnProvidedConfigEntriesIfAny()
        {
            const string file = "1/branch_file.txt";

            string path = CloneStandardTestRepo();
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

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            var options = BuildFakeSystemConfigFilemodeOption(scd, true);

            using (var repo = new Repository(path, options))
            {
                var changes = repo.Diff.Compare<TreeChanges>(new[] { file });

                Assert.Equal(1, changes.Count());

                var change = changes.Modified.Single();
                Assert.Equal(Mode.ExecutableFile, change.OldMode);
                Assert.Equal(Mode.NonExecutableFile, change.Mode);
            }

            options = BuildFakeSystemConfigFilemodeOption(scd, false);

            using (var repo = new Repository(path, options))
            {
                var changes = repo.Diff.Compare<TreeChanges>(new[] { file });

                Assert.Equal(0, changes.Count());
            }
        }

        private RepositoryOptions BuildFakeSystemConfigFilemodeOption(
            SelfCleaningDirectory scd,
            bool value)
        {
            Directory.CreateDirectory(scd.DirectoryPath);

            var options = new RepositoryOptions
                              {
                                  SystemConfigurationLocation = Path.Combine(
                                      scd.RootedDirectoryPath, "fake-system.config")
                              };

            StringBuilder sb = new StringBuilder()
                .AppendFormat("[core]{0}", Environment.NewLine)
                .AppendFormat("filemode = {1}{0}", Environment.NewLine, value);
            Touch("", options.SystemConfigurationLocation, sb.ToString());

            return options;
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
                var changes = repo.Diff.Compare<TreeChanges>(repo.Lookup<Tree>(treeOldOid), repo.Lookup<Tree>(treeNewOid));

                Assert.Equal(ChangeKind.Modified, changes["a.txt"].Status);
                Assert.Equal(ChangeKind.Modified, changes["A.TXT"].Status);
            }
        }

        [Fact]
        public void CallingCompareWithAnUnsupportedGenericParamThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Diff.Compare<string>(default(Tree), default(Tree)));
                Assert.Throws<LibGit2SharpException>(() => repo.Diff.Compare<string>());
            }
        }
    }
}
