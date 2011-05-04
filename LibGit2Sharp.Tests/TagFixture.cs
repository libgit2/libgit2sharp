using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class TagFixture
    {
        private const string newRepoPath = "new_repo";

        private readonly List<string> expectedTags = new List<string> {"test", "e90810b", "lw"};

        private static readonly Signature signatureTim = new Signature("Tim Clem", "timothy.clem@gmail.com", DateTimeOffset.UtcNow);
        private static readonly Signature signatureNtk = new Signature("nulltoken", "emeric.fermas@gmail.com", Epoch.ToDateTimeOffset(1300557894, 60));
        private const string tagTestSha = "b25fa35b38051e4ae45d4222e795f9df2e43f1d1";
        private const string commitE90810BSha = "e90810b8df3e80c413d903f631643c716887138d";
        private const string tagE90810BSha = "7b4384978d2493e851f9cca7858815fac9b10980";
        const string invalidTargetId = "deadbeef1b46c854b31185ea97743be6a8774479";

        [Test]
        public void CanCreateALightWeightTagFromSha()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("i_am_lightweight", commitE90810BSha);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeFalse();
            }
        }

        [Test]
        public void CanCreateALightweightTagFromABranchName()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("i_am_lightweight", "refs/heads/master");
                newTag.IsAnnotated.ShouldBeFalse();
                newTag.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanCreateAndOverwriteALightweightTag()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("e90810b", commitE90810BSha, true);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeFalse();
            }
        }

        [Test]
        public void CanCreateATagWithNameContainingASlash()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string lwTagName = "i/am/deep";
                var lwTag = repo.Tags.Create(lwTagName, commitE90810BSha);
                lwTag.ShouldNotBeNull();
                lwTag.IsAnnotated.ShouldBeFalse();
                lwTag.Name.ShouldEqual(lwTagName);

                const string anTagName = lwTagName + "_as_well";
                var anTag = repo.Tags.Create(anTagName, commitE90810BSha, signatureNtk, "a nice message");
                anTag.ShouldNotBeNull();
                anTag.IsAnnotated.ShouldBeTrue();
                anTag.Name.ShouldEqual(anTagName);
            }
        }

        [Test]
        public void CreatingATagWithNameMatchingAnAlreadyExistingReferenceHierarchyThows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.ApplyTag("i/am/deep");
                Assert.Throws<ApplicationException>(() => repo.ApplyTag("i/am/deep/rooted"));
                Assert.Throws<ApplicationException>(() => repo.ApplyTag("i/am"));
            }
        }

        [Test]
        public void CanCreateAnAnnotatedTagFromABranchName()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("unit_test", "refs/heads/master", signatureTim, "a new tag");
                newTag.IsAnnotated.ShouldBeTrue();
                newTag.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanCreateAnAnnotatedTagFromSha()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("unit_test", tagTestSha, signatureTim, "a new tag");
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeTrue();
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L359)")]
        public void CanCreateAnAnnotatedTagWithAnEmptyMessage()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.ApplyTag("empty-annotated-tag", signatureNtk, string.Empty);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeTrue();
                newTag.Annotation.Message.ShouldEqual(string.Empty);
            }
        }

        [Test]
        public void CanCreateAndOverwriteAnAnnotatedTag()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create("e90810b", tagTestSha, signatureTim, "a new tag", true);
                newTag.ShouldNotBeNull();
                newTag.IsAnnotated.ShouldBeTrue();
            }
        }

        [Test]
        public void CreatingAnAnnotatedTagIsDeterministic()
        {
            const string tagName = "nullTAGen";
            const string tagMessage = "I've been tagged!";

            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newTag = repo.Tags.Create(tagName, commitE90810BSha, signatureNtk, tagMessage);
                newTag.Target.Sha.ShouldEqual("24f6de34a108d931c6056fc4687637fe36c6bd6b");
                newTag.IsAnnotated.ShouldBeTrue();
                newTag.Annotation.Sha.ShouldEqual("24f6de34a108d931c6056fc4687637fe36c6bd6b");
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L32)")]
        public void CreatingATagInAEmptyRepositoryThrows()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            {
                var dir = Repository.Init(newRepoPath);

                using (var repo = new Repository(dir))
                {
                    Assert.Throws<ApplicationException>(() => repo.ApplyTag("mynotag"));
                }
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L37)")]
        public void CreatingATagForHeadInAEmptyRepositoryThrows()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            {
                var dir = Repository.Init(newRepoPath);

                using (var repo = new Repository(dir))
                {
                    Assert.Throws<ApplicationException>(() => repo.ApplyTag("mytaghead", "HEAD"));
                }
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L42)")]
        public void CreatingATagForAnUnknowReferenceShouldFail()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ApplicationException>(() => repo.ApplyTag("mytagnorev", "aaaaaaaaaaa"));
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L42)")]
        public void CreatingATagForAnUnknowObjectIdShouldFail()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ApplicationException>(() => repo.ApplyTag("mytagnorev", invalidTargetId));
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L48)")]
        public void CanCreateATagForImplicitHead()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var tag = repo.ApplyTag("mytag");
                tag.ShouldNotBeNull();

                tag.Target.Id.ShouldEqual(repo.Head.ResolveToDirectReference().Target.Id);

                var retrievedTag = repo.Tags[tag.CanonicalName];
                tag.ShouldEqual(retrievedTag);
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L87)")]
        public void CreatingADuplicateTagShouldFail()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.ApplyTag("mytag");

                Assert.Throws<ApplicationException>(() => repo.ApplyTag("mytag"));
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L90)")]
        public void CreatingATagWithANonValidNameShouldFail()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentException>(() => repo.ApplyTag(""));
                Assert.Throws<ApplicationException>(() => repo.ApplyTag(".othertag"));
                Assert.Throws<ApplicationException>(() => repo.ApplyTag("other tag"));
                Assert.Throws<ApplicationException>(() => repo.ApplyTag("othertag^"));
                Assert.Throws<ApplicationException>(() => repo.ApplyTag("other~tag"));
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L101)")]
        public void CanCreateATagUsingHead()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var tag = repo.ApplyTag("mytag", "HEAD");
                tag.ShouldNotBeNull();

                tag.Target.Id.ShouldEqual(repo.Head.ResolveToDirectReference().Target.Id);

                var retrievedTag = repo.Tags[tag.CanonicalName];
                tag.ShouldEqual(retrievedTag);
            }
        }

        [Test]
        public void CanCreateATagPointingToATree()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var headCommit = (Commit)repo.Head.ResolveToDirectReference().Target;
                var tree = headCommit.Tree;

                var tag = repo.ApplyTag("tree-tag", tree.Sha);
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeFalse();
                tag.Target.Id.ShouldEqual(tree.Id);

                repo.Lookup(tag.Target.Id).ShouldEqual(tree);
                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Test]
        public void CanCreateATagPointingToABlob()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var headCommit = (Commit)repo.Head.ResolveToDirectReference().Target;
                var blob = headCommit.Tree.Files.First();

                var tag = repo.ApplyTag("blob-tag", blob.Sha);
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeFalse();
                tag.Target.Id.ShouldEqual(blob.Id);

                repo.Lookup(tag.Target.Id).ShouldEqual(blob);
                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Test]
        public void CreatingALightweightTagPointingToATagAnnotationGeneratesAnAnnotatedTagReusingThePointedAtTagAnnotation()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var annotatedTag = repo.Tags["e90810b"];
                var annotation = annotatedTag.Annotation;

                var tag = repo.ApplyTag("lightweight-tag", annotation.Sha);
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeTrue();
                tag.Target.Id.ShouldEqual(annotation.Id);
                tag.Annotation.ShouldEqual(annotation);

                repo.Lookup(tag.Target.Id).ShouldEqual(annotation);
                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Test]
        public void CanCreateAnAnnotatedTagPointingToATagAnnotation()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var annotatedTag = repo.Tags["e90810b"];
                var annotation = annotatedTag.Annotation;

                var tag = repo.ApplyTag("annotatedtag-tag", annotation.Sha, signatureNtk, "A new annotation");
                tag.ShouldNotBeNull();
                tag.IsAnnotated.ShouldBeTrue();
                tag.Annotation.TargetId.ShouldEqual(annotation.Id);
                tag.Annotation.ShouldNotEqual(annotation);

                repo.Lookup(tag.Target.Id).ShouldEqual(tag.Annotation);
                repo.Tags[tag.Name].ShouldEqual(tag);
            }
        }

        [Test]
        public void BlindlyCreatingALightweightTagOverAnExistingOneThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Tags.Create("e90810b", "refs/heads/br2"));
            }
        }

        [Test]
        public void BlindlyCreatingAnAnnotatedTagOverAnExistingOneThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Tags.Create("e90810b", "refs/heads/br2", signatureNtk, "a nice message"));
            }
        }
        
        [Test]
        public void CreateTagWithADuplicateNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Tags.Create("test", tagTestSha, signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithEmptyNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create(string.Empty, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithEmptyTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Tags.Create("test_tag", string.Empty, signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithNotExistingTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Tags.Create("test_tag", invalidTargetId, signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithNullMessageThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", "refs/heads/master", signatureTim, null));
            }
        }

        [Test]
        public void CreateTagWithNullNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create(null, "refs/heads/master", signatureTim, "message"));
            }
        }

        [Test]
        public void CreateTagWithNullSignatureThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", "refs/heads/master", null, "message"));
            }
        }

        [Test]
        public void CreateTagWithNullTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Tags.Create("test_tag", null, signatureTim, "message"));
            }
        }

        [Test]
        public void CanDeleteATagThroughItsName()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Tags.Delete("e90810b");
            }
        }


        [Test]
        public void CanDeleteATagThroughItsCanonicalName()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Tags.Delete("refs/tags/e90810b");
            }
        }

        [Test]
        public void ADeletedTagCannotBeLookedUp()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string tagName = "e90810b";

                repo.Tags.Delete(tagName);
                repo.Tags[tagName].ShouldBeNull();
            }
        }

        [Test]
        public void DeletingATagDecreasesTheTagsCount()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string tagName = "e90810b";

                var tags = repo.Tags.Select(r => r.Name).ToList();
                tags.Contains(tagName).ShouldBeTrue();

                repo.Tags.Delete(tagName);

                var tags2 = repo.Tags.Select(r => r.Name).ToList();
                tags2.Contains(tagName).ShouldBeFalse();

                tags2.Count.ShouldEqual(tags.Count - 1);
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L108)")]
        public void DeletingAnUnknownTagShouldFail()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Tags.Delete("unknown-tag"));
            }
        }

        [Test]
        public void CanListTags()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var tag in repo.Tags)
                {
                    expectedTags.Contains(tag.Name).ShouldBeTrue();
                }
                repo.Tags.Count().ShouldEqual(3);
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L24)")]
        public void CanListAllTagsInAEmptyRepository()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            {
                var dir = Repository.Init(newRepoPath);

                using (var repo = new Repository(dir))
                {
                    repo.Info.IsEmpty.ShouldBeTrue();
                    repo.Tags.Count().ShouldEqual(0);
                }
            }
        }

        [Test]
        [Description("Ported from cgit (https://github.com/git/git/blob/1c08bf50cfcf924094eca56c2486a90e2bf1e6e2/t/t7004-tag.sh#L165)")]
        public void ListAllTagsShouldOutputThemInAnOrderedWay()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                List<string> tagNames = repo.Tags.Select(t => t.Name).ToList();
                
                var sortedTags = expectedTags;
                sortedTags.Sort();

                CollectionAssert.AreEqual(sortedTags, tagNames);
            }
        }

        [Test]
        public void CanLookupALightweightTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tag = repo.Tags["lw"];
                tag.ShouldNotBeNull();
                tag.Name.ShouldEqual("lw");
                tag.Target.Sha.ShouldEqual(commitE90810BSha);

                tag.IsAnnotated.ShouldBeFalse();
                tag.Annotation.ShouldBeNull();
            }
        }

        [Test]
        public void CanLookupATagByItsCanonicalName()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tag = repo.Tags["refs/tags/lw"];
                tag.ShouldNotBeNull();
                tag.Name.ShouldEqual("lw");

                var tag2 = repo.Tags["refs/tags/lw"];
                tag2.ShouldNotBeNull();
                tag2.Name.ShouldEqual("lw");

                tag2.ShouldEqual(tag);
                (tag2 == tag).ShouldBeTrue();
            }
        }

        [Test]
        public void CanLookupAnAnnotatedTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var tag = repo.Tags["e90810b"];
                tag.ShouldNotBeNull();
                tag.Name.ShouldEqual("e90810b");
                tag.Target.Sha.ShouldEqual(tagE90810BSha);

                tag.IsAnnotated.ShouldBeTrue();
                tag.Annotation.Sha.ShouldEqual(tagE90810BSha);
                tag.Annotation.Tagger.Email.ShouldEqual("tanoku@gmail.com");
                tag.Annotation.Tagger.Name.ShouldEqual("Vicent Marti");
                tag.Annotation.Tagger.When.ShouldEqual(DateTimeOffset.Parse("2010-08-12 03:59:17 +0200"));
                tag.Annotation.Message.ShouldEqual("This is a very simple tag.\n");
                tag.Annotation.TargetId.Sha.ShouldEqual(commitE90810BSha);
            }
        }

        [Test]
        public void LookupEmptyTagNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => { var t = repo.Tags[string.Empty]; });
            }
        }

        [Test]
        public void LookupNullTagNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { var t = repo.Tags[null]; });
            }
        }
    }
}