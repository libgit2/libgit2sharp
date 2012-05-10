using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class TagFixture : BaseFixture
    {
        private readonly string[] expectedTags = new[] { "e90810b", "lw", "point_to_blob", "test", };

        private static readonly Signature signatureTim = new Signature("Tim Clem", "timothy.clem@gmail.com", DateTimeOffset.UtcNow);
        private static readonly Signature signatureNtk = new Signature("nulltoken", "emeric.fermas@gmail.com", Epoch.ToDateTimeOffset(1300557894, 60));
        private const string tagTestSha = "b25fa35b38051e4ae45d4222e795f9df2e43f1d1";
        private const string commitE90810BSha = "e90810b8df3e80c413d903f631643c716887138d";
        private const string tagE90810BSha = "7b4384978d2493e851f9cca7858815fac9b10980";

        [Fact]
        public void CanCreateALightWeightTagFromSha()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create("i_am_lightweight", commitE90810BSha);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeFalse();
            }
        }

        [Fact]
        public void CanCreateALightWeightTagFromAbbreviatedSha()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create("i_am_lightweight", commitE90810BSha.Substring(0, 17));
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeFalse();
            }
        }

        [Fact]
        public void CanCreateALightweightTagFromABranchName()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create("i_am_lightweight", "refs/heads/master");
                newTag.IsAnnotated.ShouldBeFalse();
                newTag.ShouldNotBeNull();
            }
        }

        [Fact]
        public void CanCreateAndOverwriteALightweightTag()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create("e90810b", commitE90810BSha, true);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeFalse();
            }
        }

        [Fact]
        public void CanCreateATagWithNameContainingASlash()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string lwTagName = "i/am/deep";
                Tag lwTag = repo.Tags.Create(lwTagName, commitE90810BSha);
                lwTag.ShouldNotBeNull();
                lwTag.IsAnnotated.ShouldBeFalse();
                lwTag.Target.Sha.ShouldEqual(commitE90810BSha);
                lwTag.Name.ShouldEqual(lwTagName);

                const string anTagName = lwTagName + "_as_well";
                Tag anTag = repo.Tags.Create(anTagName, commitE90810BSha, signatureNtk, "a nice message");
                anTag.ShouldNotBeNull();
                anTag.IsAnnotated.ShouldBeTrue();
                anTag.Target.Sha.ShouldEqual(commitE90810BSha);
                anTag.Annotation.Target.ShouldEqual(anTag.Target);
                anTag.Name.ShouldEqual(anTagName);
            }
        }

        [Fact]
        public void CreatingATagWithNameMatchingAnAlreadyExistingReferenceHierarchyThrows()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.ApplyTag("i/am/deep");
                Assert.Throws<LibGit2Exception>(() => repo.ApplyTag("i/am/deep/rooted"));
                Assert.Throws<LibGit2Exception>(() => repo.ApplyTag("i/am"));
            }
        }

        [Fact]
        public void CanCreateAnAnnotatedTagFromABranchName()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create("unit_test", "refs/heads/master", signatureTim, "a new tag");
                newTag.IsAnnotated.ShouldBeTrue();
                newTag.ShouldNotBeNull();
            }
        }

        [Fact]
        public void CanCreateAnAnnotatedTagFromSha()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create("unit_test", tagTestSha, signatureTim, "a new tag");
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeTrue();
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L359)
        public void CanCreateAnAnnotatedTagWithAnEmptyMessage()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.ApplyTag("empty-annotated-tag", signatureNtk, string.Empty);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeTrue();
                newTag.Annotation.Message.ShouldEqual(string.Empty);
            }
        }

        [Fact]
        public void CanCreateAndOverwriteAnAnnotatedTag()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create("e90810b", tagTestSha, signatureTim, "a new tag", true);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeTrue();
            }
        }

        [Fact]
        public void CreatingAnAnnotatedTagIsDeterministic()
        {
            const string tagName = "nullTAGen";
            const string tagMessage = "I've been tagged!\n";

            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag newTag = repo.Tags.Create(tagName, commitE90810BSha, signatureNtk, tagMessage);
                newTag.Target.Sha.ShouldEqual(commitE90810BSha);
                newTag.IsAnnotated.ShouldBeTrue();
                newTag.Annotation.Sha.ShouldEqual("26623eee75440d63e10dcb752b88a0004c914161");
                newTag.Annotation.Target.Sha.ShouldEqual(commitE90810BSha);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L32)
        public void CreatingATagInAEmptyRepositoryThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.ApplyTag("mynotag"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L37)
        public void CreatingATagForHeadInAEmptyRepositoryThrows()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.ApplyTag("mytaghead", "HEAD"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L42)
        public void CreatingATagForAnUnknowReferenceThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.ApplyTag("mytagnorev", "aaaaaaaaaaa"));
            }
        }

        [Fact]
        public void CreatingATagForANonCanonicalReferenceThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.ApplyTag("noncanonicaltarget", "br2"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L42)
        public void CreatingATagForAnUnknowObjectIdThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.ApplyTag("mytagnorev", Constants.UnknownSha));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L48)
        public void CanCreateATagForImplicitHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag tag = repo.ApplyTag("mytag");
                tag.ShouldNotBeNull();

                tag.Target.Id.ShouldEqual(repo.Head.Tip.Id);

                Tag retrievedTag = repo.Tags[tag.CanonicalName];
                tag.ShouldEqual(retrievedTag);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L87)
        public void CreatingADuplicateTagThrows()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.ApplyTag("mytag");

                Assert.Throws<LibGit2Exception>(() => repo.ApplyTag("mytag"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L90)
        public void CreatingATagWithANonValidNameShouldFail()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.ApplyTag(""));
                Assert.Throws<LibGit2Exception>(() => repo.ApplyTag(".othertag"));
                Assert.Throws<LibGit2Exception>(() => repo.ApplyTag("other tag"));
                Assert.Throws<LibGit2Exception>(() => repo.ApplyTag("othertag^"));
                Assert.Throws<LibGit2Exception>(() => repo.ApplyTag("other~tag"));
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L101)
        public void CanCreateATagUsingHead()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag tag = repo.ApplyTag("mytag", "HEAD");
                tag.ShouldNotBeNull();

                tag.Target.Id.ShouldEqual(repo.Head.Tip.Id);

                Tag retrievedTag = repo.Tags[tag.CanonicalName];
                tag.ShouldEqual(retrievedTag);
            }
        }

        [Fact]
        public void CanCreateATagPointingToATree()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var headCommit = repo.Head.Tip;
                Tree tree = headCommit.Tree;

                Tag tag = repo.ApplyTag("tree-tag", tree.Sha);
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeFalse();
                tag.Target.Id.ShouldEqual(tree.Id);

                repo.Lookup(tag.Target.Id).ShouldEqual(tree);
                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Fact]
        public void CanCreateATagPointingToABlob()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var headCommit = repo.Head.Tip;
                Blob blob = headCommit.Tree.Blobs.First();

                Tag tag = repo.ApplyTag("blob-tag", blob.Sha);
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeFalse();
                tag.Target.Id.ShouldEqual(blob.Id);

                repo.Lookup(tag.Target.Id).ShouldEqual(blob);
                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Fact]
        public void CreatingALightweightTagPointingToATagAnnotationGeneratesAnAnnotatedTagReusingThePointedAtTagAnnotation()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag annotatedTag = repo.Tags["e90810b"];
                TagAnnotation annotation = annotatedTag.Annotation;

                Tag tag = repo.ApplyTag("lightweight-tag", annotation.Sha);
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeTrue();
                tag.Target.Id.ShouldEqual(annotation.Target.Id);
                tag.Annotation.ShouldEqual(annotation);

                repo.Lookup(tag.Annotation.Id).ShouldEqual(annotation);
                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Fact]
        public void CanCreateAnAnnotatedTagPointingToATagAnnotation()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag annotatedTag = repo.Tags["e90810b"];
                TagAnnotation annotation = annotatedTag.Annotation;

                Tag tag = repo.ApplyTag("annotatedtag-tag", annotation.Sha, signatureNtk, "A new annotation");
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeTrue();
                tag.Annotation.Target.Id.ShouldEqual(annotation.Id);
                tag.Annotation.ShouldNotEqual(annotation);

                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Fact]
        public void BlindlyCreatingALightweightTagOverAnExistingOneThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Tags.Create("e90810b", "refs/heads/br2"));
            }
        }

        [Fact]
        public void BlindlyCreatingAnAnnotatedTagOverAnExistingOneThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Tags.Create("e90810b", "refs/heads/br2", signatureNtk, "a nice message"));
            }
        }

        [Fact]
        public void CreateTagWithADuplicateNameThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Tags.Create("test", tagTestSha, signatureTim, "message"));
            }
        }

        [Fact]
        public void CreateTagWithEmptyNameThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create(string.Empty, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Fact]
        public void CreateTagWithEmptyTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create("test_tag", string.Empty, signatureTim, "message"));
            }
        }

        [Fact]
        public void CreateTagWithNotExistingTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Tags.Create("test_tag", Constants.UnknownSha, signatureTim, "message"));
            }
        }

        [Fact]
        public void CreateTagWithNullMessageThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", "refs/heads/master", signatureTim, null));
            }
        }

        [Fact]
        public void CreateTagWithNullNameThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create(null, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Fact]
        public void CreateTagWithNullSignatureThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", "refs/heads/master", null, "message"));
            }
        }

        [Fact]
        public void CreateTagWithNullTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", null, signatureTim, "message"));
            }
        }

        [Fact]
        public void CanDeleteATagThroughItsName()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Tags.Delete("e90810b");
            }
        }

        [Fact]
        public void CanDeleteATagThroughItsCanonicalName()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Tags.Delete("refs/tags/e90810b");
            }
        }

        [Fact]
        public void ADeletedTagCannotBeLookedUp()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string tagName = "e90810b";

                repo.Tags.Delete(tagName);
                repo.Tags[tagName].ShouldBeNull();
            }
        }

        [Fact]
        public void DeletingATagDecreasesTheTagsCount()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string tagName = "e90810b";

                List<string> tags = repo.Tags.Select(r => r.Name).ToList();
                tags.Contains(tagName).ShouldBeTrue();

                repo.Tags.Delete(tagName);

                List<string> tags2 = repo.Tags.Select(r => r.Name).ToList();
                tags2.Contains(tagName).ShouldBeFalse();

                tags2.Count.ShouldEqual(tags.Count - 1);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L108)
        public void DeletingAnUnknownTagShouldFail()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Tags.Delete("unknown-tag"));
            }
        }

        [Fact]
        public void GetTagByNameWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Tag tag;
                Assert.Throws<ArgumentNullException>(() => tag = repo.Tags[null]);
                Assert.Throws<ArgumentException>(() => tag = repo.Tags[""]);
            }
        }

        [Fact]
        public void CanListTags()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedTags, repo.Tags.Select(t => t.Name).ToArray());

                repo.Tags.Count().ShouldEqual(4);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L24)
        public void CanListAllTagsInAEmptyRepository()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                repo.Info.IsEmpty.ShouldBeTrue();
                repo.Tags.Count().ShouldEqual(0);
            }
        }

        [Fact]
        // Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L165)
        public void ListAllTagsShouldOutputThemInAnOrderedWay()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                List<string> tagNames = repo.Tags.Select(t => t.Name).ToList();

                List<string> sortedTags = expectedTags.ToList();
                sortedTags.Sort();

                Assert.Equal(sortedTags, tagNames);
            }
        }

        [Fact]
        public void CanLookupALightweightTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Tag tag = repo.Tags["lw"];
                tag.ShouldNotBeNull();
                tag.Name.ShouldEqual("lw");
                tag.Target.Sha.ShouldEqual(commitE90810BSha);

                tag.IsAnnotated.ShouldBeFalse();
                tag.Annotation.ShouldBeNull();
            }
        }

        [Fact]
        public void CanLookupATagByItsCanonicalName()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Tag tag = repo.Tags["refs/tags/lw"];
                tag.ShouldNotBeNull();
                tag.Name.ShouldEqual("lw");

                Tag tag2 = repo.Tags["refs/tags/lw"];
                tag2.ShouldNotBeNull();
                tag2.Name.ShouldEqual("lw");

                tag2.ShouldEqual(tag);
                (tag2 == tag).ShouldBeTrue();
            }
        }

        [Fact]
        public void CanLookupAnAnnotatedTag()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Tag tag = repo.Tags["e90810b"];
                tag.ShouldNotBeNull();
                tag.Name.ShouldEqual("e90810b");
                tag.Target.Sha.ShouldEqual(commitE90810BSha);

                tag.IsAnnotated.ShouldBeTrue();
                tag.Annotation.Sha.ShouldEqual(tagE90810BSha);
                tag.Annotation.Tagger.Email.ShouldEqual("tanoku@gmail.com");
                tag.Annotation.Tagger.Name.ShouldEqual("Vicent Marti");
                tag.Annotation.Tagger.When.ShouldEqual(DateTimeOffset.Parse("2010-08-12 03:59:17 +0200"));
                tag.Annotation.Message.ShouldEqual("This is a very simple tag.\n");
                tag.Annotation.Target.Sha.ShouldEqual(commitE90810BSha);
            }
        }

        [Fact]
        public void LookupEmptyTagNameThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => { Tag t = repo.Tags[string.Empty]; });
            }
        }

        [Fact]
        public void LookupNullTagNameThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { Tag t = repo.Tags[null]; });
            }
        }
    }
}
