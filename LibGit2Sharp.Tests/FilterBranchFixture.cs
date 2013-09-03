using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class FilterBranchFixture : BaseFixture
    {
        private readonly Repository repo;

        public FilterBranchFixture()
        {
            string path = CloneBareTestRepo();
            repo = new Repository(path);
        }

        public override void Dispose()
        {
            repo.Dispose();
        }

        [Fact]
        public void CanRewriteHistoryWithoutChangingCommitMetadata()
        {
            var originalRefs = repo.Refs.ToList().OrderBy(r => r.CanonicalName);
            var commits = repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs }).ToArray();

            // Noop header rewriter
            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter = CommitRewriteInfo.From,
            }, commits);

            Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
            Assert.Equal(commits, repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs }).ToArray());
        }

        [Fact]
        public void CanRewriteHistoryWithoutChangingTrees()
        {
            var originalRefs = repo.Refs.ToList().OrderBy(r => r.CanonicalName);
            var commits = repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs }).ToArray();

            // Noop tree rewriter
            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitTreeRewriter = TreeDefinition.From,
            }, commits);

            Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
            Assert.Equal(commits, repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs }).ToArray());
        }

        [Fact]
        public void CanRewriteAuthorOfCommits()
        {
            var commits = repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs }).ToArray();
            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c =>
                    CommitRewriteInfo.From(c, author: new Signature("Ben Straub", "me@example.com", c.Author.When)),
            }, commits);

            var nonBackedUpRefs = repo.Refs.Where(x => !x.CanonicalName.StartsWith("refs/original"));
            Assert.Empty(repo.Commits.QueryBy(new CommitFilter { Since = nonBackedUpRefs })
                             .Where(c => c.Author.Name != "Ben Straub"));
        }

        [Fact]
        public void CanRewriteAuthorOfCommitsOnlyBeingPointedAtByTags()
        {
            var commit = repo.ObjectDatabase.CreateCommit(
                "I'm a lonesome commit", Constants.Signature, Constants.Signature,
                repo.Head.Tip.Tree, Enumerable.Empty<Commit>());

            repo.Tags.Add("so-lonely", commit);

            repo.Tags.Add("so-lonely-but-annotated", commit, Constants.Signature,
                "Yeah, baby! I'm going to be rewritten as well");

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: "Bam!"),
            }, commit);

            var lightweightTag = repo.Tags["so-lonely"];
            Assert.Equal("Bam!\n", ((Commit)lightweightTag.Target).Message);

            var annotatedTag = repo.Tags["so-lonely-but-annotated"];
            Assert.Equal("Bam!\n", ((Commit)annotatedTag.Target).Message);
        }

        [Fact]
        public void CanRewriteTrees()
        {
            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitTreeRewriter =
                    c => TreeDefinition.From(c)
                                       .Remove("README"),
            }, repo.Head.Commits);

            Assert.True(repo.Head.Commits.All(c => c["README"] == null));
        }

        // git log --graph --oneline --name-status --decorate
        //
        // * 4c062a6 (HEAD, master) directory was added
        // | A     1/branch_file.txt
        // *   be3563a Merge branch 'br2'
        // |\
        // | * c47800c branch commit one
        // | | A   branch_file.txt
        // * | 9fd738e a fourth commit
        // | | M   new.txt
        // * | 4a202b3 (packed-test) a third commit
        // |/
        // |   M   README
        // * 5b5b025 another commit
        // | A     new.txt
        // * 8496071 testing
        //   A     README
        [Theory]

        // * 6d96779 (HEAD, master) directory was added
        // | A     1/branch_file.txt
        // *   ca7a3ed Merge branch 'br2'
        // |\
        // | * da8d9d0 branch commit one
        // | | A   branch_file.txt
        // * | 38f9fac a fourth commit
        // |/
        // |   M   new.txt
        // * ded26fd (packed-test) another commit
        //   A     new.txt
        [InlineData(new[] { "README" }, 5, "6d96779")]

        // * dfb164b (HEAD, master) directory was added
        // | A     1/branch_file.txt
        // *   8ab4a5f Merge branch 'br2'
        // |\
        // | * 23dd639 branch commit one
        // | | A   branch_file.txt
        // * | 5222c0f (packed-test) a third commit
        // |/
        // |   M   README
        // * 8496071 testing
        //   A     README
        [InlineData(new[] { "new.txt" }, 5, "dfb164b")]

        // * f9ee587 (HEAD, master) directory was added
        // | A     1/branch_file.txt
        // * b87858a branch commit one
        //   A     branch_file.txt
        //
        // NB: packed-test is gone
        [InlineData(new[] { "new.txt", "README" }, 2, "f9ee587")]

        // * 446fde5 (HEAD, master) directory was added
        // | A     1/branch_file.txt
        // * 9fd738e a fourth commit
        // | M     new.txt
        // * 4a202b3 (packed-test) a third commit
        // | M     README
        // * 5b5b025 another commit
        // | A     new.txt
        // * 8496071 testing
        //   A     README
        [InlineData(new[] { "branch_file.txt" }, 5, "446fde5")]

        // *   be3563a (HEAD, master) Merge branch 'br2'
        // |\
        // | * c47800c branch commit one
        // | | A   branch_file.txt
        // * | 9fd738e a fourth commit
        // | | M   new.txt
        // * | 4a202b3 (packed-test) a third commit
        // |/
        // |   M   README
        // * 5b5b025 another commit
        // | A     new.txt
        // * 8496071 testing
        //   A     README
        [InlineData(new[] { "1" }, 6, "be3563a")]

        // If all trees are empty, master should be an orphan
        [InlineData(new[] { "1", "branch_file.txt", "new.txt", "README"  }, 0, null)]
        public void CanPruneEmptyCommits(string[] treeEntriesToRemove, int expectedCommitCount, string expectedHead)
        {
            Assert.Equal(7, repo.Head.Commits.Count());

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                PruneEmptyCommits = true,
                CommitTreeRewriter =
                    c => TreeDefinition.From(c)
                                       .Remove(treeEntriesToRemove),
            }, repo.Head.Commits);

            Assert.Equal(expectedCommitCount, repo.Head.Commits.Count());

            if (expectedHead == null)
            {
                Assert.Null(repo.Head.Tip);
            }
            else
            {
                Assert.Equal(expectedHead, repo.Head.Tip.Id.Sha.Substring(0, expectedHead.Length));
            }

            foreach (var treeEntry in treeEntriesToRemove)
            {
                Assert.True(repo.Head.Commits.All(c => c[treeEntry] == null), "Did not expect a tree entry at " + treeEntry);
            }
        }

        // * 41bc8c6  (packed)
        // |
        // * 5001298            <----- rewrite this commit message
        [Fact]
        public void OnlyRewriteSelectedCommits()
        {
            var commit = repo.Branches["packed"].Tip;
            var parent = commit.Parents.Single();

            Assert.True(parent.Sha.StartsWith("5001298"));
            Assert.NotEqual(Constants.Signature, commit.Author);
            Assert.NotEqual(Constants.Signature, parent.Author);

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, author: Constants.Signature),
            }, parent);

            commit = repo.Branches["packed"].Tip;
            parent = commit.Parents.Single();

            Assert.False(parent.Sha.StartsWith("5001298"));
            Assert.NotEqual(Constants.Signature, commit.Author);
            Assert.Equal(Constants.Signature, parent.Author);
        }

        [Theory]
        [InlineData("refs/rewritten")]
        [InlineData("refs/rewritten/")]
        public void CanCustomizeTheNamespaceOfBackedUpRefs(string backupRefsNamespace)
        {
            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: ""),
            }, repo.Head.Commits);
            Assert.NotEmpty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/original")));

            Assert.Empty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/rewritten")));

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                BackupRefsNamespace = backupRefsNamespace,
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: "abc"),
            }, repo.Head.Commits);

            Assert.NotEmpty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/rewritten")));
        }

        [Fact]
        public void RefRewritingRollsBackOnFailure()
        {
            IList<Reference> origRefs = repo.Refs.OrderBy(r => r.CanonicalName).ToArray();

            const string backupNamespace = "refs/original/";

            Assert.Throws<Exception>(
                () =>
                repo.Refs.RewriteHistory(new RewriteHistoryOptions
                {
                    BackupRefsNamespace = backupNamespace,
                    CommitHeaderRewriter =
                        c => CommitRewriteInfo.From(c, message: ""),
                    TagNameRewriter =
                        (n, a, t) =>
                        {
                            var newRef1 =
                                repo.Refs.FromGlob(backupNamespace + "*").FirstOrDefault();

                            if (newRef1 == null)
                                return n;

                            // At least one of the refs have been rewritten
                            // Let's make sure it's been updated to a new target
                            var oldName1 = newRef1.CanonicalName.Replace(backupNamespace, "refs/");
                            Assert.NotEqual(origRefs.Single(r => r.CanonicalName == oldName1),
                                            newRef1);

                            // Violently interrupt the process
                            throw new Exception("BREAK");
                        },
                }, repo.Lookup<Commit>("6dcf9bf7541ee10456529833502442f385010c3d"))
                );

            // Ensure all the refs have been restored to their original targets
            var newRefs = repo.Refs.OrderBy(r => r.CanonicalName).ToArray();
            Assert.Equal(newRefs, origRefs);
        }

        // This test should rewrite br2, but not packed-test:
        // *   a4a7dce (br2) Merge branch 'master' into br2
        // |\
        // | * 9fd738e a fourth commit
        // | * 4a202b3 (packed-test) a third commit
        // * | c47800c branch commit one                <----- rewrite this one
        // |/
        // * 5b5b025 another commit
        // * 8496071 testing
        [Fact]
        public void DoesNotRewriteRefsThatDontChange()
        {
            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: "abc"),
            }, repo.Lookup<Commit>("c47800c"));

            Assert.Null(repo.Refs["refs/original/heads/packed-test"]);
            Assert.NotNull(repo.Refs["refs/original/heads/br2"]);

            // Ensure br2 is still a merge commit
            var parents = repo.Branches["br2"].Tip.Parents.ToList();
            Assert.Equal(2, parents.Count());
            Assert.NotEmpty(parents.Where(c => c.Sha.StartsWith("9fd738e")));
            Assert.Equal("abc\n", parents.Single(c => !c.Sha.StartsWith("9fd738e")).Message);
        }

        [Fact]
        public void CanNotOverWriteBackedUpReferences()
        {
            Assert.Empty(repo.Refs.FromGlob("refs/original/*"));

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: "abc"),
            }, repo.Head.Commits);
            var originalRefs = repo.Refs.FromGlob("refs/original/*").OrderBy(r => r.CanonicalName).ToArray();
            Assert.NotEmpty(originalRefs);

            Assert.Throws<InvalidOperationException>(
                () =>
                repo.Refs.RewriteHistory(new RewriteHistoryOptions
                {
                    CommitHeaderRewriter =
                        c => CommitRewriteInfo.From(c, message: "def"),
                }, repo.Head.Commits)
                );
            Assert.Equal("abc\n", repo.Head.Tip.Message);

            var newOriginalRefs = repo.Refs.FromGlob("refs/original/*").OrderBy(r => r.CanonicalName).ToArray();
            Assert.Equal(originalRefs, newOriginalRefs);

            Assert.Empty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/original/original/")));
        }

        [Fact]
        public void CanNotOverWriteAnExistingReference()
        {
            var commits = repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs }).ToArray();

            Assert.Throws<NameConflictException>(
                () =>
                repo.Refs.RewriteHistory(new RewriteHistoryOptions
                {
                    TagNameRewriter =
                        (n, a, t) => "test",
                }, commits)
                );

            Assert.Equal(0, repo.Refs.FromGlob("refs/original/*").Count());
        }

        // Graft the orphan "test" branch to the tip of "packed"
        //
        // Before:
        // * e90810b  (test, lw, e90810b, test)
        // |
        // * 6dcf9bf
        //     <------------------ note: no connection
        // * 41bc8c6  (packed)
        // |
        // * 5001298
        //
        // ... and after:
        //
        // * f558880  (test, lw, e90810b, test)
        // |
        // * 0c25efa
        // |   <------------------ add this link
        // * 41bc8c6  (packed)
        // |
        // * 5001298
        [Fact]
        public void CanRewriteParents()
        {
            var commitToRewrite = repo.Lookup<Commit>("6dcf9bf");
            var newParent = repo.Lookup<Commit>("41bc8c6");
            bool hasBeenCalled = false;

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitParentsRewriter =
                    c =>
                    {
                        Assert.False(hasBeenCalled);
                        Assert.Empty(c.Parents);
                        hasBeenCalled = true;
                        return new[] { newParent };
                    },
            }, commitToRewrite);

            Assert.Contains(newParent, repo.Lookup<Commit>("refs/heads/test~").Parents);
            Assert.True(hasBeenCalled);
        }

        // Graft the orphan "test" branch to the tip of "packed"
        //
        // Before:
        // * e90810b  (test, lw, e90810b, test)
        // |
        // * 6dcf9bf
        //     <------------------ note: no connection
        // * 41bc8c6  (packed)
        // |
        // * 5001298
        //
        // ... and after:
        //
        // * f558880  (test, lw, e90810b, test)
        // |
        // * 0c25efa
        // |   <------------------ add this link
        // * 41bc8c6  (packed)
        // |
        // * 5001298
        [Fact]
        public void CanRewriteParentWithRewrittenCommit()
        {
            var commitToRewrite = repo.Lookup<Commit>("6dcf9bf");
            var newParent = repo.Lookup<Commit>("41bc8c6");

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c =>
                    c.Id != newParent.Id
                        ? null
                        : CommitRewriteInfo.From(c, message: "changed"),
                CommitParentsRewriter =
                    c =>
                    c.Id != commitToRewrite.Id
                        ? c.Parents
                        : new[] { newParent }
            }, commitToRewrite, newParent);

            var rewrittenParent = repo.Lookup<Commit>("refs/heads/test~").Parents.Single();
            Assert.Equal(newParent.Tree, rewrittenParent.Tree);
            Assert.NotEqual(newParent, rewrittenParent);
        }

        [Fact]
        public void WritesCorrectReflogMessagesForSimpleRewrites()
        {
            EnableRefLog(repo);

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: ""),
            }, repo.Head.Commits);

            Assert.Equal("filter-branch: rewrite", repo.Refs.Log(repo.Refs["refs/heads/master"]).First().Message);
            Assert.Equal("filter-branch: backup", repo.Refs.Log(repo.Refs["refs/original/heads/master"]).First().Message);
        }

        [Fact]
        public void CanProvideNewNamesForTags()
        {
            GitObject lwTarget = repo.Tags["lw"].Target;
            GitObject e908Target = repo.Tags["e90810b"].Target;
            GitObject testTarget = repo.Tags["test"].Target;

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: ""),
                TagNameRewriter = TagNameRewriter,
            }, repo.Commits.QueryBy(new CommitFilter { Since = repo.Refs["refs/heads/test"] }));

            Assert.NotEqual(lwTarget, repo.Tags["lw_new_e90810b"].Target);
            Assert.NotEqual(e908Target, repo.Tags["e90810b_new_7b43849"].Target);
            Assert.NotEqual(testTarget, repo.Tags["test_new_b25fa35"].Target);
        }

        [Fact]
        public void CanRewriteSymbolicRefsPointingToTags()
        {
            const string tagRefName = "refs/tags/test";

            repo.Refs.Add("refs/tags/one_tracker", tagRefName);
            repo.Refs.Add("refs/tags/another_tracker", tagRefName);
            repo.Refs.Add("refs/attic/dusty_tracker", "refs/tags/another_tracker");

            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, author: Constants.Signature),
                TagNameRewriter = TagNameRewriter,
            }, repo.Lookup<Commit>("e90810b8df"));

            // Ensure the initial tags don't exist anymore...
            Assert.Null(repo.Refs["refs/tags/one_tracker"]);
            Assert.Null(repo.Refs["refs/tags/another_tracker"]);

            // ...and have been backed up.
            Assert.Equal(tagRefName, repo.Refs["refs/original/tags/one_tracker"].TargetIdentifier);
            Assert.Equal(tagRefName, repo.Refs["refs/original/tags/another_tracker"].TargetIdentifier);

            // Ensure the renamed symref tags points to the renamed target
            const string renamedTarget = "refs/tags/test_new_b25fa35";
            Assert.Equal(renamedTarget, repo.Refs["refs/tags/one_tracker_new_test"].TargetIdentifier);
            Assert.Equal(renamedTarget, repo.Refs["refs/tags/another_tracker_new_test"].TargetIdentifier);

            // Ensure that the non tag symref points to a renamed target...
            Assert.Equal("refs/tags/another_tracker_new_test", repo.Refs["refs/attic/dusty_tracker"].TargetIdentifier);

            // ...and has been backed up as well.
            Assert.Equal("refs/tags/another_tracker", repo.Refs["refs/original/attic/dusty_tracker"].TargetIdentifier);
        }

        [Fact]
        public void HandlesNameRewritingOfChainedTags()
        {
            // Add a lightweight tag (A) that points to tag annotation (B) that points to another tag annotation (C),
            // which points to a commit
            var theCommit = repo.Lookup<Commit>("6dcf9bf");
            var annotationC = repo.ObjectDatabase.CreateTagAnnotation("annotationC", theCommit, Constants.Signature, "");
            var annotationB = repo.ObjectDatabase.CreateTagAnnotation("annotationB", annotationC, Constants.Signature, "");
            var tagA = repo.Tags.Add("lightweightA", annotationB);

            // Rewrite the commit, renaming the tag
            repo.Refs.RewriteHistory(new RewriteHistoryOptions
            {
                BackupRefsNamespace = "refs/original/",
                CommitHeaderRewriter =
                    c => CommitRewriteInfo.From(c, message: "Rewrote"),
                TagNameRewriter = TagNameRewriter,
            }, repo.Lookup<Commit>("6dcf9bf"));

            // Verify the rewritten tag-annotation chain
            var newTagA = repo.Tags["lightweightA_new_d53d92e"];
            Assert.NotNull(newTagA);
            Assert.NotEqual(newTagA, tagA);
            Assert.True(newTagA.IsAnnotated);

            var newAnnotationB = newTagA.Annotation;
            Assert.NotNull(newAnnotationB);
            Assert.NotEqual(newAnnotationB, annotationB);
            Assert.Equal("annotationB_ann_237c1b0", newAnnotationB.Name);

            var newAnnotationC = newAnnotationB.Target as TagAnnotation;
            Assert.NotNull(newAnnotationC);
            Assert.NotEqual(newAnnotationC, annotationC);
            Assert.Equal("annotationC_ann_6dcf9bf", newAnnotationC.Name);

            var newCommit = newAnnotationC.Target as Commit;
            Assert.NotNull(newCommit);
            Assert.NotEqual(newCommit, theCommit);
            Assert.Equal("Rewrote\n", newCommit.Message);

            // Ensure the original tag doesn't exist anymore
            Assert.Null(repo.Tags["lightweightA"]);

            // ...but it's been backed up
            Reference backedUpTag = repo.Refs["refs/original/tags/lightweightA"];
            Assert.NotNull(backedUpTag);
            Assert.Equal(annotationB, backedUpTag.ResolveToDirectReference().Target);
        }

        private static string TagNameRewriter(string name, bool isAnnotated, string target)
        {
            const string tagPrefix = "refs/tags/";
            var t = target == null
                        ? ""
                        : target.StartsWith(tagPrefix)
                              ? target.Substring(tagPrefix.Length)
                              : target.Substring(0, 7);

            return name + (isAnnotated ? "_ann_" : "_new_") + t;
        }
    }
}
