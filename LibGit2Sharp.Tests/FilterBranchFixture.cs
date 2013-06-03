using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

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
            var commits = repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray();

            // Noop header rewriter
            repo.Refs.RewriteHistory(commits, commitHeaderRewriter: CommitRewriteInfo.From);
            Assert.Equal(originalRefs,
                         repo.Refs.Where(x => !x.CanonicalName.StartsWith("refs/original"))
                             .OrderBy(r => r.CanonicalName)
                             .ToList());
            Assert.Equal(commits, repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray());
        }

        [Fact]
        public void CanRewriteHistoryWithoutChangingTrees()
        {
            var originalRefs = repo.Refs.ToList().OrderBy(r => r.CanonicalName);
            var commits = repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray();

            // Noop tree rewriter
            repo.Refs.RewriteHistory(commits, commitTreeRewriter: TreeDefinition.From);

            Assert.Equal(originalRefs,
                         repo.Refs.Where(x => !x.CanonicalName.StartsWith("refs/original"))
                             .OrderBy(r => r.CanonicalName)
                             .ToList());
            Assert.Equal(commits, repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray());
        }

        [Fact]
        public void CanRewriteAuthorOfCommitsNotBeingPointedAtByTags()
        {
            var commits = repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray();
            repo.Refs.RewriteHistory(
                commits,
                commitHeaderRewriter: c => CommitRewriteInfo.From(c, author: new Signature("Ben Straub", "me@example.com", c.Author.When)));

            var nonTagRefs = repo.Refs.Where(x => !x.IsTag()).Where(x => !x.CanonicalName.StartsWith("refs/original"));
            Assert.Empty(repo.Commits.QueryBy(new Filter { Since = nonTagRefs })
                             .Where(c => c.Author.Name != "Ben Straub"));
        }

        [Fact]
        public void CanRewriteTrees()
        {
            repo.Refs.RewriteHistory(repo.Head.Commits, commitTreeRewriter: c =>
                {
                    var td = TreeDefinition.From(c);
                    td.Remove("README");
                    return td;
                });

            Assert.Empty(repo.Head.Commits.Where(c => c["README"] != null));
        }

        [Fact]
        public void CanCustomizeRefRewriting()
        {
            repo.Refs.RewriteHistory(repo.Head.Commits, c => CommitRewriteInfo.From(c, message: ""));
            Assert.NotEmpty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/original")));

            Assert.Empty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/rewritten")));
            repo.Refs.RewriteHistory(repo.Head.Commits,
                                     commitHeaderRewriter: c => CommitRewriteInfo.From(c, message: "abc"),
                                     backupRefsNamespace: "refs/rewritten");
            Assert.NotEmpty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/rewritten")));
        }

        [Fact]
        public void RefRewritingRollsBackOnFailure()
        {
            Dictionary<string, Reference> origRefs = repo.Refs.ToDictionary(r => r.CanonicalName);

            const string backupNamespace = "refs/original/";

            Assert.Throws<Exception>(
                () =>
                repo.Refs.RewriteHistory(
                    new[] { repo.Lookup<Commit>("6dcf9bf7541ee10456529833502442f385010c3d") },
                    c => CommitRewriteInfo.From(c, message: ""),
                    backupRefsNamespace: backupNamespace,
                    tagNameRewriter: (n, isA, t) =>
                                            {
                                                var newRef = repo.Refs.FromGlob(backupNamespace + "*").FirstOrDefault();

                                                if (newRef == null)
                                                    return n;

                                                // At least one of the refs have been rewritten
                                                // Let's make sure it's been updated to a new target
                                                var oldName = newRef.CanonicalName.Replace(backupNamespace, "refs/");
                                                Assert.NotEqual(origRefs[oldName], newRef);

                                                // Violently interrupt the process
                                                throw new Exception("BREAK");
                                            }
                    ));

            // Ensure all the refs have been restored to their original targets
            var newRefs = repo.Refs.Where(x => !x.CanonicalName.StartsWith(backupNamespace)).ToArray();
            Assert.Equal(newRefs, origRefs.Values);
        }

        [Fact]
        public void TagRewritingRollsBackOnFailure()
        {
            var origTags = repo.Tags.ToArray();

            Assert.Throws<Exception>(
                () =>
                repo.Refs.RewriteHistory(
                    repo.Commits.QueryBy(new Filter { Since = repo.Refs }),
                    c => CommitRewriteInfo.From(c, message: ""),
                    tagNameRewriter: (oldName, isAnnotated, newTarget) =>
                        {
                            if (oldName == "e90810b")
                            {
                                throw new Exception("BREAK");
                            }
                            // Move tags
                            return oldName;
                        }));

            var newTags = repo.Tags.ToArray();

            Assert.Equal(origTags, newTags);
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
            repo.Refs.RewriteHistory(new[] { repo.Lookup<Commit>("c47800c") },
                                c => CommitRewriteInfo.From(c, message: "abc"));
            Assert.Null(repo.Refs["refs/original/heads/packed-test"]);
            Assert.NotNull(repo.Refs["refs/original/heads/br2"]);
        }

        [Fact]
        public void HandlesExistingBackedUpRefs()
        {
            Func<Commit, CommitRewriteInfo> headerRewriter = c => CommitRewriteInfo.From(c, message: "abc");

            repo.Refs.RewriteHistory(repo.Head.Commits, commitHeaderRewriter: headerRewriter);
            Assert.Throws<InvalidOperationException>(() =>
                repo.Refs.RewriteHistory(repo.Head.Commits, commitHeaderRewriter: headerRewriter));
            Assert.Empty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/original/original/")));
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

            repo.Refs.RewriteHistory(new[] { commitToRewrite }, parentRewriter: originalParents =>
            {
                Assert.False(hasBeenCalled);
                Assert.Empty(originalParents);
                hasBeenCalled = true;
                return new[] { newParent };
            });

            Assert.Contains(newParent, repo.Lookup<Commit>("refs/heads/test~").Parents);
            Assert.True(hasBeenCalled);
        }

        [Fact]
        public void WritesCorrectReflogMessagesForSimpleRewrites()
        {
            repo.Refs.RewriteHistory(repo.Head.Commits, c => CommitRewriteInfo.From(c, message: ""));

            Assert.Equal("filter-branch: rewrite", repo.Refs.Log(repo.Refs["refs/heads/master"]).First().Message);
            Assert.Equal("filter-branch: backup", repo.Refs.Log(repo.Refs["refs/original/heads/master"]).First().Message);
        }

        [Fact]
        public void DoesntMoveTagsByDefault()
        {
            var originalTags = repo.Tags.ToArray();
            repo.Refs.RewriteHistory(repo.Commits.QueryBy(new Filter { Since = repo.Refs["refs/heads/test"] }),
                                     c => CommitRewriteInfo.From(c, message: ""));
            var newTags = repo.Tags.ToArray();
            Assert.Equal(originalTags, newTags);
        }

        [Fact]
        public void CanLeaveTagsAlone()
        {
            var origTags = repo.Tags.ToArray();
            repo.Refs.RewriteHistory(repo.Commits.QueryBy(new Filter { Since = repo.Refs["refs/heads/test"] }),
                         c => CommitRewriteInfo.From(c, message: ""),
                         tagNameRewriter: (oldName, isAnnotated, o) => null);
            var newTags = repo.Tags.ToArray();
            Assert.Equal(origTags, newTags);
        }

        [Fact]
        public void CanProvideNewNamesForTags()
        {
            repo.Refs.RewriteHistory(repo.Commits.QueryBy(new Filter { Since = repo.Refs["refs/heads/test"] }),
                                     c => CommitRewriteInfo.From(c, message: ""),
                                     tagNameRewriter: (oldName, isAnnotated, o) => oldName + "_new");
            Assert.NotEqual(repo.Tags["lw"].Target, repo.Tags["lw_new"].Target);
            Assert.NotEqual(repo.Tags["e90810b"].Target, repo.Tags["e90810b_new"].Target);
            Assert.NotEqual(repo.Tags["test"].Target, repo.Tags["test_new"].Target);
        }

        [Fact]
        public void HandlesChainedTags()
        {
            // Add a tag (A) that points to another tag (B)
            repo.Tags.Add("chained", repo.Tags["e90810b"].Annotation,
                          new Signature("Me", "me@example.com", DateTimeOffset.Now),
                          "Chained tag");
            repo.Refs.RewriteHistory(repo.Commits.QueryBy(new Filter { Since = repo.Refs["refs/heads/test"] }),
                                     c => CommitRewriteInfo.From(c, message: ""),
                                     tagNameRewriter: (oldName, isAnnotated, o) => oldName + "_new");
            // Verify the new (A) points to the new (B)
            ObjectId newTarget = repo.Tags["e90810b_new"].Annotation.Id;
            ObjectId newChainedTarget = repo.Tags["chained_new"].Target.Id;
            Assert.Equal(newTarget, newChainedTarget);
        }
    }
}
