using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class TagFixture : BaseFixture
    {
        private readonly string[] expectedTags = new[] { "e90810b", "lw", "point_to_blob", "tag_without_tagger", "test", };

        private static readonly Signature signatureTim = new Signature("Tim Clem", "timothy.clem@gmail.com", TruncateSubSeconds(DateTimeOffset.UtcNow));
        private static readonly Signature signatureNtk = new Signature("nulltoken", "emeric.fermas@gmail.com", DateTimeOffset.FromUnixTimeSeconds(1300557894).ToOffset(TimeSpan.FromMinutes(60)));
        private const string tagTestSha = "b25fa35b38051e4ae45d4222e795f9df2e43f1d1";
        private const string commitE90810BSha = "e90810b8df3e80c413d903f631643c716887138d";
        private const string tagE90810BSha = "7b4384978d2493e851f9cca7858815fac9b10980";

        [Fact]
        public void CanAddALightWeightTagFromSha()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("i_am_lightweight", commitE90810BSha);
                Assert.NotNull(newTag);
                Assert.False(newTag.IsAnnotated);
                Assert.Equal(commitE90810BSha, newTag.Target.Sha);
            }
        }

        [Fact]
        public void CanAddALightWeightTagFromAGitObject()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                GitObject obj = repo.Lookup(commitE90810BSha);

                Tag newTag = repo.Tags.Add("i_am_lightweight", obj);
                Assert.NotNull(newTag);
                Assert.False(newTag.IsAnnotated);
                Assert.Equal(commitE90810BSha, newTag.Target.Sha);
            }
        }

        [Fact]
        public void CanAddALightWeightTagFromAbbreviatedSha()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("i_am_lightweight", commitE90810BSha.Substring(0, 17));
                Assert.NotNull(newTag);
                Assert.False(newTag.IsAnnotated);
            }
        }

        [Fact]
        public void CanAddALightweightTagFromABranchName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("i_am_lightweight", "refs/heads/master");
                Assert.False(newTag.IsAnnotated);
                Assert.NotNull(newTag);
            }
        }

        [Fact]
        public void CanAddALightweightTagFromARevparseSpec()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("i_am_lightweight", "master^1^2");
                Assert.False(newTag.IsAnnotated);
                Assert.NotNull(newTag);
                Assert.Equal("c47800c7266a2be04c571c04d5a6614691ea99bd", newTag.Target.Sha);
            }
        }

        [Fact]
        public void CanAddAndOverwriteALightweightTag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("e90810b", commitE90810BSha, true);
                Assert.NotNull(newTag);
                Assert.False(newTag.IsAnnotated);
            }
        }

        [Fact]
        public void CanAddATagWithNameContainingASlash()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string lwTagName = "i/am/deep";
                Tag lwTag = repo.Tags.Add(lwTagName, commitE90810BSha);
                Assert.NotNull(lwTag);
                Assert.False(lwTag.IsAnnotated);
                Assert.Equal(commitE90810BSha, lwTag.Target.Sha);
                Assert.Equal(lwTagName, lwTag.FriendlyName);

                const string anTagName = lwTagName + "_as_well";
                Tag anTag = repo.Tags.Add(anTagName, commitE90810BSha, signatureNtk, "a nice message");
                Assert.NotNull(anTag);
                Assert.True(anTag.IsAnnotated);
                Assert.Equal(commitE90810BSha, anTag.Target.Sha);
                Assert.Equal(anTag.Target, anTag.Annotation.Target);
                Assert.Equal(anTagName, anTag.FriendlyName);
            }
        }

        [Fact]
        public void CreatingATagWithNameMatchingAnAlreadyExistingReferenceHierarchyThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.ApplyTag("i/am/deep");
                Assert.Throws<LibGit2SharpException>(() => repo.ApplyTag("i/am/deep/rooted"));
                Assert.Throws<LibGit2SharpException>(() => repo.ApplyTag("i/am"));
            }
        }

        [Fact]
        public void CanAddAnAnnotatedTagFromABranchName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("unit_test", "refs/heads/master", signatureTim, "a new tag");
                Assert.True(newTag.IsAnnotated);
                Assert.NotNull(newTag);
            }
        }

        [Fact]
        public void CanAddAnAnnotatedTagFromSha()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("unit_test", tagTestSha, signatureTim, "a new tag");
                Assert.NotNull(newTag);
                Assert.True(newTag.IsAnnotated);
                Assert.Equal(tagTestSha, newTag.Target.Sha);
                Assert.Equal(signatureTim, newTag.Annotation.Tagger);
            }
        }

        [Fact]
        public void CanAddAnAnnotatedTagFromObject()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                GitObject obj = repo.Lookup(tagTestSha);

                Tag newTag = repo.Tags.Add("unit_test", obj, signatureTim, "a new tag");
                Assert.NotNull(newTag);
                Assert.True(newTag.IsAnnotated);
                Assert.Equal(tagTestSha, newTag.Target.Sha);
            }
        }

        [Fact]
        public void CanAddAnAnnotatedTagFromARevparseSpec()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("unit_test", "master^1^2", signatureTim, "a new tag");
                Assert.NotNull(newTag);
                Assert.True(newTag.IsAnnotated);
                Assert.Equal("c47800c7266a2be04c571c04d5a6614691ea99bd", newTag.Target.Sha);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L359)
        public void CanAddAnAnnotatedTagWithAnEmptyMessage()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.ApplyTag("empty-annotated-tag", signatureNtk, string.Empty);
                Assert.NotNull(newTag);
                Assert.True(newTag.IsAnnotated);
                Assert.Equal(string.Empty, newTag.Annotation.Message);
            }
        }

        [Fact]
        public void CanAddAndOverwriteAnAnnotatedTag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add("e90810b", tagTestSha, signatureTim, "a new tag", true);
                Assert.NotNull(newTag);
                Assert.True(newTag.IsAnnotated);
            }
        }

        [Fact]
        public void CreatingAnAnnotatedTagIsDeterministic()
        {
            const string tagName = "nullTAGen";
            const string tagMessage = "I've been tagged!";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag newTag = repo.Tags.Add(tagName, commitE90810BSha, signatureNtk, tagMessage);
                Assert.Equal(commitE90810BSha, newTag.Target.Sha);
                Assert.True(newTag.IsAnnotated);
                Assert.Equal("26623eee75440d63e10dcb752b88a0004c914161", newTag.Annotation.Sha);
                Assert.Equal(commitE90810BSha, newTag.Annotation.Target.Sha);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L32)
        public void CreatingATagInAEmptyRepositoryThrows()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.Throws<UnbornBranchException>(() => repo.ApplyTag("mynotag"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L37)
        public void CreatingATagForHeadInAEmptyRepositoryThrows()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.Throws<UnbornBranchException>(() => repo.ApplyTag("mytaghead", "HEAD"));
                Assert.Throws<UnbornBranchException>(() => repo.ApplyTag("mytaghead"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L42)
        public void CreatingATagForAnUnknowReferenceThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.ApplyTag("mytagnorev", "aaaaaaaaaaa"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L42)
        public void CreatingATagForAnUnknowObjectIdThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.ApplyTag("mytagnorev", Constants.UnknownSha));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L48)
        public void CanAddATagForImplicitHead()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.ApplyTag("mytag");
                Assert.NotNull(tag);

                Assert.Equal(repo.Head.Tip.Id, tag.Target.Id);

                Tag retrievedTag = repo.Tags[tag.CanonicalName];
                Assert.Equal(retrievedTag, tag);
            }
        }

        [Fact]
        public void CanAddATagForImplicitHeadInDetachedState()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Commands.Checkout(repo, repo.Head.Tip);

                Assert.True(repo.Info.IsHeadDetached);

                Tag tag = repo.ApplyTag("mytag");
                Assert.NotNull(tag);

                Assert.Equal(repo.Head.Tip.Id, tag.Target.Id);

                Tag retrievedTag = repo.Tags[tag.CanonicalName];
                Assert.Equal(retrievedTag, tag);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L87)
        public void CreatingADuplicateTagThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.ApplyTag("mytag");

                Assert.Throws<NameConflictException>(() => repo.ApplyTag("mytag"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L90)
        public void CreatingATagWithANonValidNameShouldFail()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.ApplyTag(""));
                Assert.Throws<InvalidSpecificationException>(() => repo.ApplyTag(".othertag"));
                Assert.Throws<InvalidSpecificationException>(() => repo.ApplyTag("other tag"));
                Assert.Throws<InvalidSpecificationException>(() => repo.ApplyTag("othertag^"));
                Assert.Throws<InvalidSpecificationException>(() => repo.ApplyTag("other~tag"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L101)
        public void CanAddATagUsingHead()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.ApplyTag("mytag", "HEAD");
                Assert.NotNull(tag);

                Assert.Equal(repo.Head.Tip.Id, tag.Target.Id);

                Tag retrievedTag = repo.Tags[tag.CanonicalName];
                Assert.Equal(retrievedTag, tag);
            }
        }

        [Fact]
        public void CanAddATagPointingToATree()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Commit headCommit = repo.Head.Tip;
                Tree tree = headCommit.Tree;

                Tag tag = repo.ApplyTag("tree-tag", tree.Sha);
                Assert.NotNull(tag);
                Assert.False(tag.IsAnnotated);
                Assert.Equal(tree.Id, tag.Target.Id);

                Assert.Equal(tree, repo.Lookup(tag.Target.Id));
                Assert.Equal(tag, repo.Tags[tag.FriendlyName]);
            }
        }

        [Fact]
        public void CanReadTagWithoutTagger()
        {
            // Not all tags have a tagger.
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["tag_without_tagger"];

                Assert.True(tag.IsAnnotated);
                Assert.NotNull(tag.Target);
                Assert.Null(tag.Annotation.Tagger);

                Tree tree = repo.Lookup<Tree>("581f9824ecaf824221bd36edf5430f2739a7c4f5");
                Assert.NotNull(tree);

                Assert.Equal(tree.Id, tag.Target.Id);
            }
        }

        [Fact]
        public void CanAddATagPointingToABlob()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("a823312");

                Tag tag = repo.ApplyTag("blob-tag", blob.Sha);
                Assert.NotNull(tag);
                Assert.False(tag.IsAnnotated);
                Assert.Equal(blob.Id, tag.Target.Id);

                Assert.Equal(blob, repo.Lookup(tag.Target.Id));
                Assert.Equal(tag, repo.Tags[tag.FriendlyName]);
            }
        }

        [Fact]
        public void CreatingALightweightTagPointingToATagAnnotationGeneratesAnAnnotatedTagReusingThePointedAtTagAnnotation()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag annotatedTag = repo.Tags["e90810b"];
                TagAnnotation annotation = annotatedTag.Annotation;

                Tag tag = repo.ApplyTag("lightweight-tag", annotation.Sha);
                Assert.NotNull(tag);
                Assert.True(tag.IsAnnotated);
                Assert.Equal(annotation.Target.Id, tag.Target.Id);
                Assert.Equal(annotation, tag.Annotation);

                Assert.Equal(annotation, repo.Lookup(tag.Annotation.Id));
                Assert.Equal(tag, repo.Tags[tag.FriendlyName]);
            }
        }

        [Fact]
        public void CanAddAnAnnotatedTagPointingToATagAnnotation()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag annotatedTag = repo.Tags["e90810b"];
                TagAnnotation annotation = annotatedTag.Annotation;

                Tag tag = repo.ApplyTag("annotatedtag-tag", annotation.Sha, signatureNtk, "A new annotation");
                Assert.NotNull(tag);
                Assert.True(tag.IsAnnotated);
                Assert.Equal(annotation.Id, tag.Annotation.Target.Id);
                Assert.NotEqual(annotation, tag.Annotation);

                Assert.Equal(tag, repo.Tags[tag.FriendlyName]);
            }
        }

        [Fact]
        public void BlindlyCreatingALightweightTagOverAnExistingOneThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Tags.Add("e90810b", "refs/heads/br2"));
            }
        }

        [Fact]
        public void BlindlyCreatingAnAnnotatedTagOverAnExistingOneThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Tags.Add("e90810b", "refs/heads/br2", signatureNtk, "a nice message"));
            }
        }

        [Fact]
        public void AddTagWithADuplicateNameThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Tags.Add("test", tagTestSha, signatureTim, "message"));
            }
        }

        [Fact]
        public void AddTagWithEmptyNameThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Add(string.Empty, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Fact]
        public void AddTagWithEmptyTargetThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Add("test_tag", string.Empty, signatureTim, "message"));
            }
        }

        [Fact]
        public void AddTagWithNotExistingTargetThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Tags.Add("test_tag", Constants.UnknownSha, signatureTim, "message"));
            }
        }

        [Fact]
        public void AddTagWithNullMessageThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Add("test_tag", "refs/heads/master", signatureTim, null));
            }
        }

        [Fact]
        public void AddTagWithNullNameThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Add(null, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Fact]
        public void AddTagWithNullSignatureThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Add("test_tag", "refs/heads/master", null, "message"));
            }
        }

        [Fact]
        public void AddTagWithNullTargetThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Add("test_tag", (GitObject)null, signatureTim, "message"));
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Add("test_tag", (string)null, signatureTim, "message"));
            }
        }

        [Fact]
        public void CanRemoveATagThroughItsName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Tags.Remove("e90810b");
            }
        }

        [Fact]
        public void CanRemoveATagThroughItsCanonicalName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Tags.Remove("refs/tags/e90810b");
            }
        }

        [Fact]
        public void CanRemoveATag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["e90810b"];
                repo.Tags.Remove(tag);
            }
        }

        [Fact]
        public void ARemovedTagCannotBeLookedUp()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string tagName = "e90810b";

                repo.Tags.Remove(tagName);
                Assert.Null(repo.Tags[tagName]);
            }
        }

        [Fact]
        public void RemovingATagDecreasesTheTagsCount()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string tagName = "e90810b";

                List<string> tags = repo.Tags.Select(r => r.FriendlyName).ToList();
                Assert.Contains(tagName, tags);

                repo.Tags.Remove(tagName);

                List<string> tags2 = repo.Tags.Select(r => r.FriendlyName).ToList();
                Assert.DoesNotContain(tagName, tags2);

                Assert.Equal(tags.Count - 1, tags2.Count);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L108)
        public void RemovingAnUnknownTagShouldFail()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Tags.Remove("unknown-tag"));
            }
        }

        [Fact]
        public void GetTagByNameWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag;
                Assert.Throws<ArgumentNullException>(() => tag = repo.Tags[null]);
                Assert.Throws<ArgumentException>(() => tag = repo.Tags[""]);
            }
        }

        [Fact]
        public void CanListTags()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedTags, SortedTags(repo.Tags, t => t.FriendlyName));

                Assert.Equal(5, repo.Tags.Count());
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L24)
        public void CanListAllTagsInAEmptyRepository()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.True(repo.Info.IsHeadUnborn);
                Assert.Empty(repo.Tags);
            }
        }

        [Fact]
        public void CanLookupALightweightTag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["lw"];
                Assert.NotNull(tag);
                Assert.Equal("lw", tag.FriendlyName);
                Assert.Equal(commitE90810BSha, tag.Target.Sha);

                Assert.False(tag.IsAnnotated);
                Assert.Null(tag.Annotation);
            }
        }

        [Fact]
        public void CanLookupATagByItsCanonicalName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["refs/tags/lw"];
                Assert.NotNull(tag);
                Assert.Equal("lw", tag.FriendlyName);

                Tag tag2 = repo.Tags["refs/tags/lw"];
                Assert.NotNull(tag2);
                Assert.Equal("lw", tag2.FriendlyName);

                Assert.Equal(tag, tag2);
                Assert.True((tag2 == tag));
            }
        }

        [Fact]
        public void CanLookupAnAnnotatedTag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["e90810b"];
                Assert.NotNull(tag);
                Assert.Equal("e90810b", tag.FriendlyName);
                Assert.Equal(commitE90810BSha, tag.Target.Sha);

                Assert.True(tag.IsAnnotated);
                Assert.Equal(tagE90810BSha, tag.Annotation.Sha);
                Assert.Equal("tanoku@gmail.com", tag.Annotation.Tagger.Email);
                Assert.Equal("Vicent Marti", tag.Annotation.Tagger.Name);
                Assert.Equal(DateTimeOffset.Parse("2010-08-12 03:59:17 +0200"), tag.Annotation.Tagger.When);
                Assert.Equal("This is a very simple tag.\n", tag.Annotation.Message);
                Assert.Equal(commitE90810BSha, tag.Annotation.Target.Sha);
            }
        }

        [Fact]
        public void LookupEmptyTagNameThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => { Tag t = repo.Tags[string.Empty]; });
            }
        }

        [Fact]
        public void LookupNullTagNameThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => { Tag t = repo.Tags[null]; });
            }
        }

        [Fact]
        public void CanRetrieveThePeeledTargetOfATagPointingToATag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["test"];

                Assert.True(tag.Target is TagAnnotation);
                Assert.True(tag.PeeledTarget is Commit);
            }
        }

        [Theory]
        [InlineData("e90810b")]
        [InlineData("lw")]
        [InlineData("point_to_blob")]
        [InlineData("tag_without_tagger")]
        public void PeeledTargetAndTargetAreEqualWhenTagIsNotChained(string tagName)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags[tagName];

                Assert.Equal<GitObject>(tag.Target, tag.PeeledTarget);
            }
        }

        private static T[] SortedTags<T>(IEnumerable<Tag> tags, Func<Tag, T> selector)
        {
            return tags.OrderBy(t => t.CanonicalName, StringComparer.Ordinal).Select(selector).ToArray();
        }
    }
}
