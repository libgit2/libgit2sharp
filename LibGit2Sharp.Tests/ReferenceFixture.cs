using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ReferenceFixture : BaseFixture
    {
        private readonly string[] expectedRefs = new[]
                                                     {
                                                         "refs/heads/br2", "refs/heads/deadbeef", "refs/heads/master", "refs/heads/packed", "refs/heads/packed-test",
                                                         "refs/heads/test", "refs/notes/answer", "refs/notes/answer2", "refs/notes/commits", "refs/tags/e90810b",
                                                         "refs/tags/lw", "refs/tags/point_to_blob", "refs/tags/tag_without_tagger", "refs/tags/test"
                                                     };

        [Fact]
        public void CanAddADirectReference()
        {
            const string name = "refs/heads/unit_test";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var newRef = (DirectReference)repo.Refs.Add(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", newRef.Target.Sha);
                Assert.Equal(newRef.Target.Sha, newRef.TargetIdentifier);
                Assert.NotNull(repo.Refs[name]);

                AssertRefLogEntry(repo, name,
                    "branch: Created from be3563ae3f795b2b4353bcce3a527ad0a4f7f644",
                    null, newRef.ResolveToDirectReference().Target.Id, Constants.Identity, before
                    );
            }
        }

        [Fact]
        public void CanAddADirectReferenceFromRevParseSpec()
        {
            const string name = "refs/heads/extendedShaSyntaxRulz";
            const string logMessage = "Create new ref";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var newRef = (DirectReference)repo.Refs.Add(name, "master^1^2", logMessage);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("c47800c7266a2be04c571c04d5a6614691ea99bd", newRef.Target.Sha);
                Assert.Equal(newRef.Target.Sha, newRef.TargetIdentifier);
                Assert.NotNull(repo.Refs[name]);

                AssertRefLogEntry(repo, name, logMessage,
                                  null, newRef.ResolveToDirectReference().Target.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void CreatingADirectReferenceWithARevparseSpecPointingAtAnUnknownObjectFails()
        {
            const string name = "refs/heads/extendedShaSyntaxRulz";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Refs.Add(name, "master^42"));
            }
        }

        [Fact]
        public void CanAddASymbolicReferenceFromTheTargetName()
        {
            const string name = "refs/heads/unit_test";
            const string target = "refs/heads/master";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var newRef = (SymbolicReference)repo.Refs.Add(name, target);

                AssertSymbolicRef(newRef, repo, target, name);
            }
        }

        [Fact]
        public void CanAddASymbolicReferenceFromTheTargetReference()
        {
            const string name = "refs/heads/unit_test";
            const string target = "refs/heads/master";
            const string logMessage = "unit_test reference init";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                var targetRef = repo.Refs[target];

                var newRef = repo.Refs.Add(name, targetRef, logMessage);

                AssertSymbolicRef(newRef, repo, target, name);
                Assert.Empty(repo.Refs.Log(newRef));
            }
        }

        private static void AssertSymbolicRef(SymbolicReference newRef, IRepository repo, string expectedTargetName, string expectedName)
        {
            Assert.NotNull(newRef);
            Assert.Equal(expectedName, newRef.CanonicalName);
            Assert.Equal(expectedTargetName, newRef.Target.CanonicalName);
            Assert.Equal(newRef.Target.CanonicalName, newRef.TargetIdentifier);
            Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", newRef.ResolveToDirectReference().Target.Sha);
            Assert.NotNull(repo.Refs[expectedName]);
        }

        [Fact]
        public void BlindlyCreatingADirectReferenceOverAnExistingOneThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Refs.Add("refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
            }
        }

        [Fact]
        public void BlindlyCreatingASymbolicReferenceOverAnExistingOneThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Refs.Add("HEAD", "refs/heads/br2"));
            }
        }

        [Fact]
        public void CanAddAndOverwriteADirectReference()
        {
            const string name = "refs/heads/br2";
            const string target = "4c062a6361ae6959e06292c1fa5e2822d9c96345";
            const string logMessage = "Create new ref";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                var oldRef = repo.Refs[name];

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var newRef = (DirectReference)repo.Refs.Add(name, target, logMessage, true);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal(target, newRef.Target.Sha);
                Assert.Equal(target, ((DirectReference)repo.Refs[name]).Target.Sha);

                AssertRefLogEntry(repo, name,
                                  logMessage, ((DirectReference)oldRef).Target.Id,
                                  newRef.ResolveToDirectReference().Target.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void CanAddAndOverwriteASymbolicReference()
        {
            const string name = "HEAD";
            const string target = "refs/heads/br2";
            const string logMessage = "Create new ref";

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                var oldtarget = repo.Refs[name].ResolveToDirectReference().Target.Id;

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var newRef = (SymbolicReference)repo.Refs.Add(name, target, logMessage, true);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("a4a7dce85cf63874e984719f4fdd239f5145052f", newRef.ResolveToDirectReference().Target.Sha);
                Assert.Equal(target, ((SymbolicReference)repo.Refs.Head).Target.CanonicalName);

                AssertRefLogEntry(repo, name, logMessage,
                                  oldtarget,
                                  newRef.ResolveToDirectReference().Target.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void AddWithEmptyStringForTargetThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Add("refs/heads/newref", string.Empty));
            }
        }

        [Fact]
        public void AddWithEmptyStringThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Add(string.Empty, "refs/heads/master"));
            }
        }

        [Fact]
        public void AddWithNullForTargetThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Add("refs/heads/newref", (string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Add("refs/heads/newref", (ObjectId)null));
            }
        }

        [Fact]
        public void AddWithNullStringThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Add(null, "refs/heads/master"));
            }
        }

        [Fact]
        public void CanRemoveAReference()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.Remove("refs/heads/packed");
            }
        }

        [Fact]
        public void CanRemoveANonExistingReference()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string unknown = "refs/heads/dahlbyk/has/hawkeyes";

                Assert.Null(repo.Refs[unknown]);
                repo.Refs.Remove(unknown);
                Assert.Null(repo.Refs[unknown]);
            }
        }

        [Fact]
        public void ARemovedReferenceCannotBeLookedUp()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string refName = "refs/heads/test";

                repo.Refs.Remove(refName);
                Assert.Null(repo.Refs[refName]);
            }
        }

        [Fact]
        public void RemovingAReferenceDecreasesTheRefsCount()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string refName = "refs/heads/test";

                List<string> refs = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.Contains(refName, refs);

                repo.Refs.Remove(refName);

                List<string> refs2 = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.DoesNotContain(refName, refs2);

                Assert.Equal(refs.Count - 1, refs2.Count);
            }
        }

        [Fact]
        public void RemoveWithEmptyNameThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Remove(string.Empty));
            }
        }

        [Fact]
        public void RemoveWithNullNameThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Remove((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Remove((Reference)null));
            }
        }

        [Fact]
        public void CanListAllReferencesEvenCorruptedOnes()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                CreateCorruptedDeadBeefHead(repo.Info.Path);

                Assert.Equal(expectedRefs, SortedRefs(repo, r => r.CanonicalName));

                Assert.Equal(14, repo.Refs.Count());
            }
        }

        [Fact]
        public void CanResolveHeadByName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var head = (SymbolicReference)repo.Refs.Head;
                Assert.NotNull(head);
                Assert.Equal("HEAD", head.CanonicalName);
                Assert.NotNull(head.Target);
                Assert.Equal("refs/heads/master", head.Target.CanonicalName);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", head.ResolveToDirectReference().Target.Sha);
                Assert.IsType<Commit>(((DirectReference)head.Target).Target);

                Branch head2 = repo.Head;
                Assert.Equal("refs/heads/master", head2.CanonicalName);
                Assert.NotNull(head2.Tip);

                Assert.Equal(head.ResolveToDirectReference().Target, head2.Tip);
            }
        }

        [Fact]
        public void CanResolveReferenceToALightweightTag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var lwTag = (DirectReference)repo.Refs["refs/tags/lw"];
                Assert.NotNull(lwTag);
                Assert.Equal("refs/tags/lw", lwTag.CanonicalName);
                Assert.NotNull(lwTag.Target);
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", lwTag.Target.Sha);
                Assert.IsType<Commit>(lwTag.Target);
            }
        }

        [Fact]
        public void CanResolveReferenceToAnAnnotatedTag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var annTag = (DirectReference)repo.Refs["refs/tags/test"];
                Assert.NotNull(annTag);
                Assert.Equal("refs/tags/test", annTag.CanonicalName);
                Assert.NotNull(annTag.Target);
                Assert.Equal("b25fa35b38051e4ae45d4222e795f9df2e43f1d1", annTag.Target.Sha);
                Assert.IsType<TagAnnotation>(annTag.Target);
            }
        }

        [Fact]
        public void CanResolveRefsByName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var master = (DirectReference)repo.Refs["refs/heads/master"];
                Assert.NotNull(master);
                Assert.Equal("refs/heads/master", master.CanonicalName);
                Assert.NotNull(master.Target);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", master.Target.Sha);
                Assert.IsType<Commit>(master.Target);
            }
        }

        [Fact]
        public void ResolvingWithEmptyStringThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => { Reference head = repo.Refs[string.Empty]; });
            }
        }

        [Fact]
        public void ResolvingWithNullThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => { Reference head = repo.Refs[null]; });
            }
        }

        [Fact]
        public void CanUpdateTargetOfADirectReference()
        {
            const string masterRef = "refs/heads/master";
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                string sha = repo.Refs["refs/heads/test"].ResolveToDirectReference().Target.Sha;
                Reference master = repo.Refs[masterRef];
                Assert.NotEqual(sha, master.ResolveToDirectReference().Target.Sha);

                Reference updated = repo.Refs.UpdateTarget(masterRef, sha);

                master = repo.Refs[masterRef];
                Assert.Equal(updated, master);

                Assert.Equal(sha, master.ResolveToDirectReference().Target.Sha);
            }
        }

        [Fact]
        public void CanUpdateTargetOfADirectReferenceWithAnAbbreviatedSha()
        {
            const string masterRef = "refs/heads/master";
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                string sha = repo.Refs["refs/heads/test"].ResolveToDirectReference().Target.Sha;
                Reference master = repo.Refs[masterRef];
                Assert.NotEqual(sha, master.ResolveToDirectReference().Target.Sha);

                Reference updated = repo.Refs.UpdateTarget(masterRef, sha.Substring(0,4));

                master = repo.Refs[masterRef];
                Assert.Equal(updated, master);

                Assert.Equal(sha, master.ResolveToDirectReference().Target.Sha);
            }
        }

        [Fact]
        public void CanUpdateTargetOfASymbolicReference()
        {
            const string name = "refs/heads/unit_test";
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var newRef = (SymbolicReference)repo.Refs.Add(name, "refs/heads/master");
                Assert.NotNull(newRef);

                repo.Refs.UpdateTarget(newRef.CanonicalName, "refs/heads/test");

                newRef = (SymbolicReference)repo.Refs[newRef.CanonicalName];
                Assert.Equal(repo.Refs["refs/heads/test"].ResolveToDirectReference().Target, newRef.ResolveToDirectReference().Target);

                repo.Refs.Remove(newRef.CanonicalName);
            }
        }

        [Fact]
        public void CanUpdateHeadWithARevparseSpec()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch test = repo.Branches["test"];

                Reference direct = repo.Refs.UpdateTarget("HEAD", test.Tip.Sha);
                Assert.True((direct is DirectReference));
                Assert.Equal(repo.Refs.Head, direct);

                Reference symref = repo.Refs.UpdateTarget("HEAD", test.CanonicalName);
                Assert.True((symref is SymbolicReference));
                Assert.Equal(repo.Refs.Head, symref);
            }
        }

        [Fact]
        public void CanUpdateHeadWithEitherAnObjectIdOrAReference()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                Reference head = repo.Refs.Head;
                Reference test = repo.Refs["refs/heads/test"];

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Reference direct = repo.Refs.UpdateTarget(head, new ObjectId(test.TargetIdentifier), null);
                Assert.True((direct is DirectReference));
                Assert.Equal(test.TargetIdentifier, direct.TargetIdentifier);
                Assert.Equal(repo.Refs.Head, direct);

                var testTargetId = test.ResolveToDirectReference().Target.Id;
                AssertRefLogEntry(repo, "HEAD", null,
                                  head.ResolveToDirectReference().Target.Id,
                                  testTargetId,
                                  Constants.Identity, before);

                const string secondLogMessage = "second update target message";

                before = DateTimeOffset.Now.TruncateMilliseconds();

                Reference symref = repo.Refs.UpdateTarget(head, test, secondLogMessage);
                Assert.True((symref is SymbolicReference));
                Assert.Equal(test.CanonicalName, symref.TargetIdentifier);
                Assert.Equal(repo.Refs.Head, symref);

                AssertRefLogEntry(repo, "HEAD",
                                  secondLogMessage,
                                  testTargetId,
                                  testTargetId,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void CanUpdateTargetOfADirectReferenceWithARevparseSpec()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                const string name = "refs/heads/master";

                var master = (DirectReference) repo.Refs[name];
                var @from = master.Target.Id;

                const string logMessage = "update target message";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var newRef = (DirectReference)repo.Refs.UpdateTarget(master, "master^1^2", logMessage);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("c47800c7266a2be04c571c04d5a6614691ea99bd", newRef.Target.Sha);
                Assert.Equal(newRef.Target.Sha, newRef.TargetIdentifier);
                Assert.NotNull(repo.Refs[name]);

                AssertRefLogEntry(repo, name,
                                  logMessage,
                                  @from,
                                  newRef.Target.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void UpdatingADirectRefWithSymbolFails()
        {
            const string name = "refs/heads/unit_test";
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var newRef = (SymbolicReference)repo.Refs.Add(name, "refs/heads/master");
                Assert.NotNull(newRef);

                Assert.Throws<ArgumentException>(
                    () => repo.Refs.UpdateTarget(newRef.CanonicalName, repo.Refs["refs/heads/test"].ResolveToDirectReference().Target.Sha));

                repo.Refs.Remove(newRef.CanonicalName);
            }
        }

        [Fact]
        public void CanUpdateTargetOfADirectReferenceWithAShortReferenceNameAsARevparseSpec()
        {
            const string masterRef = "refs/heads/master";
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Reference updatedMaster = repo.Refs.UpdateTarget(masterRef, "heads/test");
                Assert.Equal(repo.Refs["refs/heads/test"].TargetIdentifier, updatedMaster.TargetIdentifier);
            }
        }

        [Fact]
        public void UpdatingAReferenceTargetWithBadParametersFails()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.UpdateTarget(string.Empty, "refs/heads/packed"));
                Assert.Throws<ArgumentException>(() => repo.Refs.UpdateTarget("refs/heads/master", string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.UpdateTarget((string)null, "refs/heads/packed"));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.UpdateTarget((DirectReference)null, "refs/heads/packed"));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.UpdateTarget("refs/heads/master", null));
            }
        }

        [Fact]
        public void UpdatingADirectReferenceTargetWithARevparsePointingAtAnUnknownObjectFails()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Refs.UpdateTarget(repo.Refs["refs/heads/master"], "refs/heads/nope"));
            }
        }

        [Fact]
        public void CanRenameAReferenceToADeeperReferenceHierarchy()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string newName = "refs/tags/test/deep";

                Reference renamed = repo.Refs.Rename("refs/tags/test", newName);
                Assert.NotNull(renamed);
                Assert.Equal(newName, renamed.CanonicalName);
            }
        }

        [Fact]
        public void CanRenameAReferenceToAUpperReferenceHierarchy()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string newName = "refs/heads/o/sole";
                const string oldName = newName + "/mio";

                repo.Refs.Add(oldName, repo.Head.CanonicalName);
                Reference renamed = repo.Refs.Rename(oldName, newName);
                Assert.NotNull(renamed);
                Assert.Equal(newName, renamed.CanonicalName);
            }
        }

        [Fact]
        public void CanRenameAReferenceToADifferentReferenceHierarchy()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                EnableRefLog(repo);

                var oldId = repo.Refs[oldName].ResolveToDirectReference().Target.Id;

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Reference renamed = repo.Refs.Rename(oldName, newName);
                Assert.NotNull(renamed);
                Assert.Equal(newName, renamed.CanonicalName);
                Assert.Equal(oldId, renamed.ResolveToDirectReference().Target.Id);

                AssertRefLogEntry(repo, newName,
                    string.Format("reference: renamed {0} to {1}", oldName, newName),
                    oldId,
                    renamed.ResolveToDirectReference().Target.Id,
                    Constants.Identity, before);
            }
        }

        [Fact]
        public void RenamingANonExistingReferenceThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Refs.Rename("refs/tags/i-am-void", "refs/atic/tagtest"));
            }
        }

        [Fact]
        public void CanRenameAndOverWriteAExistingReference()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string oldName = "refs/heads/packed";
                const string newName = "refs/heads/br2";

                Reference renamed = repo.Refs.Rename(oldName, newName, allowOverwrite: true);

                Assert.Null(repo.Refs[oldName]);
                Assert.NotNull(repo.Refs[renamed.CanonicalName]);
            }
        }

        [Fact]
        public void BlindlyOverwritingAExistingReferenceThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Refs.Rename("refs/heads/packed", "refs/heads/br2"));
            }
        }

        [Fact]
        public void RenamingAReferenceDoesNotDecreaseTheRefsCount()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                List<string> refs = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.Contains(oldName, refs);

                repo.Refs.Rename(oldName, newName);

                List<string> refs2 = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.DoesNotContain(oldName, refs2);
                Assert.Contains(newName, refs2);

                Assert.Equal(refs2.Count, refs.Count);
            }
        }

        [Fact]
        public void CanLookupARenamedReference()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                Reference renamed = repo.Refs.Rename(oldName, newName);

                Reference lookedUp = repo.Refs[newName];
                Assert.Equal(lookedUp, renamed);
            }
        }

        [Fact]
        public void CanFilterReferencesWithAGlob()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(13, repo.Refs.FromGlob("*").Count());
                Assert.Equal(5, repo.Refs.FromGlob("refs/heads/*").Count());
                Assert.Equal(5, repo.Refs.FromGlob("refs/tags/*").Count());
                Assert.Equal(3, repo.Refs.FromGlob("*t?[pqrs]t*").Count());
                Assert.Empty(repo.Refs.FromGlob("test"));
            }
        }

        [Theory]
        [InlineData("refs/heads/master", true)]
        [InlineData("no_lowercase_as_first_level", false)]
        [InlineData("ALL_CAPS_AND_UNDERSCORE", true)]
        [InlineData("refs/stash", true)]
        [InlineData("refs/heads/pmiossec-branch", true)]
        [InlineData("refs/heads/pmiossec@{0}", false)]
        [InlineData("refs/heads/sher.lock", false)]
        [InlineData("refs/heads/sher.lock/holmes", false)]
        [InlineData("/", false)]
        public void CanTellIfAReferenceIsValid(string refname, bool expectedResult)
        {
            Assert.Equal(expectedResult, Reference.IsValidName(refname));
        }

        [Fact]
        public void CanUpdateTheTargetOfASymbolicReferenceWithAnotherSymbolicReference()
        {
            string repoPath = SandboxBareTestRepo();
            using (var repo = new Repository(repoPath))
            {
                Reference symbolicRef = repo.Refs.Add("refs/heads/unit_test", "refs/heads/master");

                Reference newHead = repo.Refs.UpdateTarget(repo.Refs.Head, symbolicRef);
                var symbolicHead = Assert.IsType<SymbolicReference>(newHead);
                Assert.Equal(symbolicRef.CanonicalName, newHead.TargetIdentifier);
                Assert.Equal(symbolicRef, symbolicHead.Target);
            }
        }

        [Fact]
        public void LookingForLowerCaseHeadThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<InvalidSpecificationException>(() => repo.Refs["head"]);
            }
        }

        private static T[] SortedRefs<T>(IRepository repo, Func<Reference, T> selector)
        {
            return repo.Refs.OrderBy(r => r.CanonicalName, StringComparer.Ordinal).Select(selector).ToArray();
        }

        [Fact]
        public void CanIdentifyReferenceKind()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.True(repo.Refs["refs/heads/master"].IsLocalBranch);
                Assert.True(repo.Refs["refs/remotes/origin/master"].IsRemoteTrackingBranch);
                Assert.True(repo.Refs["refs/tags/lw"].IsTag);
            }

            path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.True(repo.Refs["refs/notes/commits"].IsNote);
            }
        }

        [Fact]
        public void CanQueryReachability()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var result = repo.Refs.ReachableFrom(
                    new[] { repo.Lookup<Commit>("f8d44d7"), repo.Lookup<Commit>("6dcf9bf") });

                var expected = new[]
                {
                    "refs/heads/diff-test-cases",
                    "refs/heads/i-do-numbers",
                    "refs/remotes/origin/test",
                    "refs/tags/e90810b",
                    "refs/tags/lw",
                    "refs/tags/test",
                };

                Assert.Equal(expected, result.Select(x => x.CanonicalName).OrderBy(x => x).ToList());
            }
        }

        [Fact]
        public void CanQueryReachabilityAmongASubsetOfreferences()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var result = repo.Refs.ReachableFrom(
                    repo.Refs.Where(r => r.IsTag),
                    new[] { repo.Lookup<Commit>("f8d44d7"), repo.Lookup<Commit>("6dcf9bf") });

                var expected = new[]
                {
                    "refs/tags/e90810b",
                    "refs/tags/lw",
                    "refs/tags/test",
                };

                Assert.Equal(expected, result.Select(x => x.CanonicalName).OrderBy(x => x).ToList());
            }
        }

        [Fact]
        public void CanHandleInvalidArguments()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.ReachableFrom(null));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.ReachableFrom(null, repo.Commits.Take(2)));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.ReachableFrom(repo.Refs, null));
                Assert.Empty(repo.Refs.ReachableFrom(Array.Empty<Commit>()));
            }
        }
    }
}
