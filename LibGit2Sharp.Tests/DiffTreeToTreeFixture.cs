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

                TreeChanges changes = repo.Diff.Compare(ancestor, tree, new[]{ "1" });
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
                             changes.Added.Select(x => x.Path).OrderBy(p => p, StringComparer.Ordinal).ToArray());

                Assert.Equal(9, changes.LinesAdded);
                Assert.Equal(2, changes.LinesDeleted);
                Assert.Equal(2, changes["readme.txt"].LinesDeleted);
            }
        }

        [Fact]
        public void CanCompareATreeAgainstAnotherTreeWithLaxExplicitPathsValidationAndNonExistentPath()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree commitTreeWithDifferentAncestor = repo.Branches["refs/remotes/origin/test"].Tip.Tree;

                TreeChanges changes = repo.Diff.Compare(commitTreeWithDifferentAncestor, commitTree,
                        new[] { "if-I-exist-this-test-is-really-unlucky.txt" }, new ExplicitPathsOptions { ShouldFailOnUnmatchedPath = false });
                Assert.Equal(0, changes.Count());

                changes = repo.Diff.Compare(commitTreeWithDifferentAncestor, commitTree,
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
                    repo.Diff.Compare(commitTreeWithDifferentAncestor, commitTree,
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

                TreeChanges changes = repo.Diff.Compare(rootCommitTree, commitTreeWithUpdatedFile,
                                                        compareOptions: new CompareOptions { ContextLines = contextLines });

                Assert.Equal(1, changes.Count());
                Assert.Equal(1, changes.Modified.Count());
                Assert.Equal(expectedPatchLength, changes.Patch.Length);

                TreeEntryChanges treeEntryChanges = changes.Modified.Single();

                Assert.Equal(2, treeEntryChanges.LinesAdded);
                Assert.Equal(1, treeEntryChanges.LinesDeleted);
                Assert.Equal(expectedPatchLength, treeEntryChanges.Patch.Length);
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
            };

            using (var repo = new Repository(StandardTestRepoPath))
            {
                Tree rootCommitTree = repo.Lookup<Commit>("f8d44d7").Tree;
                Tree mergedCommitTree = repo.Lookup<Commit>("7252fe2").Tree;

                TreeChanges changes = repo.Diff.Compare(rootCommitTree, mergedCommitTree, compareOptions: compareOptions);

                Assert.Equal(3, changes.Count());
                Assert.Equal(1, changes.Modified.Count());
                Assert.Equal(1, changes.Deleted.Count());
                Assert.Equal(1, changes.Added.Count());

                TreeEntryChanges treeEntryChanges = changes["numbers.txt"];

                Assert.Equal(3, treeEntryChanges.LinesAdded);
                Assert.Equal(1, treeEntryChanges.LinesDeleted);

                Assert.Equal(Mode.Nonexistent, changes["my-name-does-not-feel-right.txt"].Mode);
                Assert.Equal(Expected("f8d44d7...7252fe2/numbers.txt-{0}-{1}.diff", contextLines, interhunkLines),
                             treeEntryChanges.Patch);
                Assert.Equal(Expected("f8d44d7...7252fe2/full-{0}-{1}.diff", contextLines, interhunkLines),
                             changes.Patch);
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

                TreeChanges changes = repo.Diff.Compare(treeOld, treeNew);

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

        [Fact]
        public void ComparingTwoNullTreesReturnsAnEmptyTreeChanges()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                TreeChanges changes = repo.Diff.Compare(default(Tree), default(Tree));

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
                File.WriteAllBytes(fullpath, ((Blob)(entry.Target)).Content);

                // Unset the local core.filemode, if any.
                repo.Config.Unset("core.filemode", ConfigurationLevel.Local);
            }

            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            var options = BuildFakeSystemConfigFilemodeOption(scd, true);

            using (var repo = new Repository(path, options))
            {
                TreeChanges changes = repo.Diff.Compare(new []{ file });

                Assert.Equal(1, changes.Count());

                var change = changes.Modified.Single();
                Assert.Equal(Mode.ExecutableFile, change.OldMode);
                Assert.Equal(Mode.NonExecutableFile, change.Mode);
            }

            options = BuildFakeSystemConfigFilemodeOption(scd, false);

            using (var repo = new Repository(path, options))
            {
                TreeChanges changes = repo.Diff.Compare(new[] { file });

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
            File.WriteAllText(options.SystemConfigurationLocation, sb.ToString());

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
                var changes = repo.Diff.Compare(repo.Lookup<Tree>(treeOldOid), repo.Lookup<Tree>(treeNewOid));

                Assert.Equal(ChangeKind.Modified, changes["a.txt"].Status);
                Assert.Equal(ChangeKind.Modified, changes["A.TXT"].Status);
            }
        }
    }
}
