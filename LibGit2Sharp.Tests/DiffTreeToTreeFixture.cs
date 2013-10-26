                var changes = repo.Diff.Compare<TreeChanges>(tree, tree);
                var patch = repo.Diff.Compare<Patch>(tree, tree);
                Assert.Empty(patch);
                Assert.Equal(String.Empty, patch);
                var changes = repo.Diff.Compare<TreeChanges>(tree, tree);
                var changes = repo.Diff.Compare<TreeChanges>(parentCommitTree, commitTree);

                var patch = repo.Diff.Compare<Patch>(parentCommitTree, commitTree);
                Assert.False(patch["1.txt"].IsBinaryComparison);
                var changes = repo.Diff.Compare<TreeChanges>(ancestor, tree, new[] { "1" });
                var changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree);
                var patch = repo.Diff.Compare<Patch>(commitTreeWithDifferentAncestor, commitTree);
                Assert.Equal(9, patch.LinesAdded);
                Assert.Equal(2, patch.LinesDeleted);
                Assert.Equal(2, patch["readme.txt"].LinesDeleted);
                var changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                changes = repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                    repo.Diff.Compare<TreeChanges>(commitTreeWithDifferentAncestor, commitTree,
                var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, commitTreeWithRenamedFile);
                var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, commitTreeWithUpdatedFile);
                var patch = repo.Diff.Compare<Patch>(rootCommitTree, commitTreeWithUpdatedFile,
                                        compareOptions: new CompareOptions { ContextLines = contextLines });

                Assert.Equal(expectedPatchLength, patch.Content.Length);
                ContentChanges contentChanges = patch["numbers.txt"];

                Assert.Equal(2, contentChanges.LinesAdded);
                Assert.Equal(1, contentChanges.LinesDeleted);
                Assert.Equal(expectedPatchLength, contentChanges.Patch.Length);
                var changes = repo.Diff.Compare<TreeChanges>(rootCommitTree, mergedCommitTree);
                Assert.Equal(Mode.Nonexistent, changes["my-name-does-not-feel-right.txt"].Mode);
                var patch = repo.Diff.Compare<Patch>(rootCommitTree, mergedCommitTree, compareOptions: compareOptions);
                ContentChanges contentChanges = patch["numbers.txt"];

                Assert.Equal(3, contentChanges.LinesAdded);
                Assert.Equal(1, contentChanges.LinesDeleted);
                    contentChanges.Patch);
                    patch);
                var changes = repo.Diff.Compare<TreeChanges>(treeOld, treeNew);
                var changes = repo.Diff.Compare<TreeChanges>(tree, null);
                changes = repo.Diff.Compare<TreeChanges>(null, tree);
                var changes = repo.Diff.Compare<TreeChanges>(default(Tree), default(Tree));
                using (var stream = ((Blob)(entry.Target)).GetContentStream())
                {
                    Touch(repo.Info.WorkingDirectory, file, stream);
                }
                repo.Config.Unset("core.filemode");
                var changes = repo.Diff.Compare<TreeChanges>(new[] { file });
                var changes = repo.Diff.Compare<TreeChanges>(new[] { file });
                var changes = repo.Diff.Compare<TreeChanges>(repo.Lookup<Tree>(treeOldOid), repo.Lookup<Tree>(treeNewOid));

        [Fact]
        public void CallingCompareWithAnUnsupportedGenericParamThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Diff.Compare<string>(default(Tree), default(Tree)));
                Assert.Throws<LibGit2SharpException>(() => repo.Diff.Compare<string>());
            }
        }