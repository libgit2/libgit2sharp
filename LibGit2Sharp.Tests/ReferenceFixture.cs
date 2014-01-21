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
                                                         "refs/tags/lw", "refs/tags/point_to_blob", "refs/tags/test"
                                                     };

        [Fact]
        public void CanAddADirectReference()
        {
            const string name = "refs/heads/unit_test";

            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                var newRef = (DirectReference)repo.Refs.Add(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", newRef.Target.Sha);
                Assert.Equal(newRef.Target.Sha, newRef.TargetIdentifier);
                Assert.NotNull(repo.Refs[name]);

                AssertRefLogEntry(repo, name,
                    newRef.ResolveToDirectReference().Target.Id,
                    "branch: Created from be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
            }
        }

        [Fact]
        public void CanAddADirectReferenceFromRevParseSpec()
        {
            const string name = "refs/heads/extendedShaSyntaxRulz";
            const string logMessage = "Create new ref";

            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                var newRef = (DirectReference)repo.Refs.Add(name, "master^1^2", Constants.Signature, logMessage);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("c47800c7266a2be04c571c04d5a6614691ea99bd", newRef.Target.Sha);
                Assert.Equal(newRef.Target.Sha, newRef.TargetIdentifier);
                Assert.NotNull(repo.Refs[name]);

                AssertRefLogEntry(repo, name,
                                  newRef.ResolveToDirectReference().Target.Id,
                                  logMessage, committer: Constants.Signature);
            }
        }

        [Fact]
        public void CreatingADirectReferenceWithARevparseSpecPointingAtAnUnknownObjectFails()
        {
            const string name = "refs/heads/extendedShaSyntaxRulz";

            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Refs.Add(name, "master^42"));
            }
        }

        [Fact]
        public void CanAddASymbolicReferenceFromTheTargetName()
        {
            const string name = "refs/heads/unit_test";
            const string target = "refs/heads/master";

            string path = CloneBareTestRepo();
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

            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                var targetRef = repo.Refs[target];

                var newRef = repo.Refs.Add(name, targetRef, Constants.Signature, logMessage);

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
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Refs.Add("refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
            }
        }

        [Fact]
        public void BlindlyCreatingASymbolicReferenceOverAnExistingOneThrows()
        {
            string path = CloneBareTestRepo();
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

            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                var oldRef = repo.Refs[name];
                var newRef = (DirectReference)repo.Refs.Add(name, target, Constants.Signature, logMessage, true);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal(target, newRef.Target.Sha);
                Assert.Equal(target, ((DirectReference)repo.Refs[name]).Target.Sha);

                AssertRefLogEntry(repo, name,
                                  newRef.ResolveToDirectReference().Target.Id,
                                  logMessage, ((DirectReference)oldRef).Target.Id,
                                  Constants.Signature);
            }
        }

        [Fact]
        public void CanAddAndOverwriteASymbolicReference()
        {
            const string name = "HEAD";
            const string target = "refs/heads/br2";
            const string logMessage = "Create new ref";

            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                var oldtarget = repo.Refs[name].ResolveToDirectReference().Target.Id;
                var newRef = (SymbolicReference)repo.Refs.Add(name, target, Constants.Signature, logMessage, true);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("a4a7dce85cf63874e984719f4fdd239f5145052f", newRef.ResolveToDirectReference().Target.Sha);
                Assert.Equal(target, ((SymbolicReference)repo.Refs.Head).Target.CanonicalName);

                AssertRefLogEntry(repo, name,
                                  newRef.ResolveToDirectReference().Target.Id,
                                  logMessage, oldtarget,
                                  Constants.Signature);
            }
        }

        [Fact]
        public void AddWithEmptyStringForTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Add("refs/heads/newref", string.Empty));
            }
        }

        [Fact]
        public void AddWithEmptyStringThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Add(string.Empty, "refs/heads/master"));
            }
        }

        [Fact]
        public void AddWithNullForTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Add("refs/heads/newref", (string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Add("refs/heads/newref", (ObjectId)null));
            }
        }

        [Fact]
        public void AddWithNullStringThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Add(null, "refs/heads/master"));
            }
        }

        [Fact]
        public void CanRemoveAReference()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Refs.Remove("refs/heads/packed");
            }
        }

        [Fact]
        public void CanRemoveANonExistingReference()
        {
            string path = CloneBareTestRepo();
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
            string path = CloneBareTestRepo();
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
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string refName = "refs/heads/test";

                List<string> refs = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.True(refs.Contains(refName));

                repo.Refs.Remove(refName);

                List<string> refs2 = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.False(refs2.Contains(refName));

                Assert.Equal(refs.Count - 1, refs2.Count);
            }
        }

        [Fact]
        public void RemoveWithEmptyNameThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Remove(string.Empty));
            }
        }

        [Fact]
        public void RemoveWithNullNameThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Remove((string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Remove((Reference)null));
            }
        }

        [Fact]
        public void CanListAllReferencesEvenCorruptedOnes()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                CreateCorruptedDeadBeefHead(repo.Info.Path);

                Assert.Equal(expectedRefs, SortedRefs(repo, r => r.CanonicalName));

                Assert.Equal(13, repo.Refs.Count());
            }
        }

        [Fact]
        public void CanResolveHeadByName()
        {
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => { Reference head = repo.Refs[string.Empty]; });
            }
        }

        [Fact]
        public void ResolvingWithNullThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { Reference head = repo.Refs[null]; });
            }
        }

        [Fact]
        public void CanUpdateTargetOfADirectReference()
        {
            const string masterRef = "refs/heads/master";
            string path = CloneBareTestRepo();
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
            string path = CloneBareTestRepo();
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
            string path = CloneBareTestRepo();
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
            string path = CloneBareTestRepo();
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
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                Reference head = repo.Refs.Head;
                Reference test = repo.Refs["refs/heads/test"];

                Reference direct = repo.Refs.UpdateTarget(head, new ObjectId(test.TargetIdentifier), Constants.Signature, null);
                Assert.True((direct is DirectReference));
                Assert.Equal(test.TargetIdentifier, direct.TargetIdentifier);
                Assert.Equal(repo.Refs.Head, direct);

                var testTargetId = test.ResolveToDirectReference().Target.Id;
                AssertRefLogEntry(repo, "HEAD",
                                  testTargetId,
                                  null,
                                  head.ResolveToDirectReference().Target.Id,
                                  Constants.Signature);

                const string secondLogMessage = "second update target message";
                Reference symref = repo.Refs.UpdateTarget(head, test, Constants.Signature, secondLogMessage);
                Assert.True((symref is SymbolicReference));
                Assert.Equal(test.CanonicalName, symref.TargetIdentifier);
                Assert.Equal(repo.Refs.Head, symref);

                AssertRefLogEntry(repo, "HEAD",
                                  testTargetId,
                                  secondLogMessage,
                                  testTargetId,
                                  Constants.Signature);
            }
        }

        [Fact]
        public void CanUpdateTargetOfADirectReferenceWithARevparseSpec()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                const string name = "refs/heads/master";

                var master = (DirectReference) repo.Refs[name];
                var @from = master.Target.Id;

                const string logMessage = "update target message";
                var newRef = (DirectReference)repo.Refs.UpdateTarget(master, "master^1^2", Constants.Signature, logMessage);
                Assert.NotNull(newRef);
                Assert.Equal(name, newRef.CanonicalName);
                Assert.NotNull(newRef.Target);
                Assert.Equal("c47800c7266a2be04c571c04d5a6614691ea99bd", newRef.Target.Sha);
                Assert.Equal(newRef.Target.Sha, newRef.TargetIdentifier);
                Assert.NotNull(repo.Refs[name]);

                AssertRefLogEntry(repo, name,
                                  newRef.Target.Id,
                                  logMessage,
                                  @from,
                                  Constants.Signature);
            }
        }

        [Fact]
        public void UpdatingADirectRefWithSymbolFails()
        {
            const string name = "refs/heads/unit_test";
            string path = CloneBareTestRepo();
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
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Reference updatedMaster = repo.Refs.UpdateTarget(masterRef, "heads/test");
                Assert.Equal(repo.Refs["refs/heads/test"].TargetIdentifier, updatedMaster.TargetIdentifier);
            }
        }

        [Fact]
        public void UpdatingAReferenceTargetWithBadParametersFails()
        {
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Refs.UpdateTarget(repo.Refs["refs/heads/master"], "refs/heads/nope"));
            }
        }

        [Fact]
        public void CanMoveAReferenceToADeeperReferenceHierarchy()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string newName = "refs/tags/test/deep";

                Reference moved = repo.Refs.Move("refs/tags/test", newName);
                Assert.NotNull(moved);
                Assert.Equal(newName, moved.CanonicalName);
            }
        }

        [Fact]
        public void CanMoveAReferenceToAUpperReferenceHierarchy()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string newName = "refs/heads/o/sole";
                const string oldName = newName + "/mio";

                repo.Refs.Add(oldName, repo.Head.CanonicalName);
                Reference moved = repo.Refs.Move(oldName, newName);
                Assert.NotNull(moved);
                Assert.Equal(newName, moved.CanonicalName);
            }
        }

        [Fact]
        public void CanMoveAReferenceToADifferentReferenceHierarchy()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                EnableRefLog(repo);

                var oldId = repo.Refs[oldName].ResolveToDirectReference().Target.Id;

                Reference moved = repo.Refs.Move(oldName, newName);
                Assert.NotNull(moved);
                Assert.Equal(newName, moved.CanonicalName);
                Assert.Equal(oldId, moved.ResolveToDirectReference().Target.Id);

                AssertRefLogEntry(repo, newName, moved.ResolveToDirectReference().Target.Id, 
                    string.Format("reference: renamed {0} to {1}", oldName, newName));
            }
        }

        [Fact]
        public void MovingANonExistingReferenceThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Refs.Move("refs/tags/i-am-void", "refs/atic/tagtest"));
            }
        }

        [Fact]
        public void CanMoveAndOverWriteAExistingReference()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string oldName = "refs/heads/packed";
                const string newName = "refs/heads/br2";

                Reference moved = repo.Refs.Move(oldName, newName, allowOverwrite: true);

                Assert.Null(repo.Refs[oldName]);
                Assert.NotNull(repo.Refs[moved.CanonicalName]);
            }
        }

        [Fact]
        public void BlindlyOverwritingAExistingReferenceThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<NameConflictException>(() => repo.Refs.Move("refs/heads/packed", "refs/heads/br2"));
            }
        }

        [Fact]
        public void MovingAReferenceDoesNotDecreaseTheRefsCount()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                List<string> refs = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.True(refs.Contains(oldName));

                repo.Refs.Move(oldName, newName);

                List<string> refs2 = repo.Refs.Select(r => r.CanonicalName).ToList();
                Assert.False(refs2.Contains(oldName));
                Assert.True(refs2.Contains(newName));

                Assert.Equal(refs2.Count, refs.Count);
            }
        }

        [Fact]
        public void CanLookupAMovedReference()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                Reference moved = repo.Refs.Move(oldName, newName);

                Reference lookedUp = repo.Refs[newName];
                Assert.Equal(lookedUp, moved);
            }
        }

        [Fact]
        public void CanFilterReferencesWithAGlob()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(12, repo.Refs.FromGlob("*").Count());
                Assert.Equal(5, repo.Refs.FromGlob("refs/heads/*").Count());
                Assert.Equal(4, repo.Refs.FromGlob("refs/tags/*").Count());
                Assert.Equal(3, repo.Refs.FromGlob("*t?[pqrs]t*").Count());
                Assert.Equal(0, repo.Refs.FromGlob("test").Count());
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
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedResult, repo.Refs.IsValidName(refname));
            }
        }

        [Fact]
        public void CanUpdateTheTargetOfASymbolicReferenceWithAnotherSymbolicReference()
        {
            string repoPath = CloneBareTestRepo();

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
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.True(repo.Refs["refs/heads/master"].IsLocalBranch());
                Assert.True(repo.Refs["refs/remotes/origin/master"].IsRemoteTrackingBranch());
                Assert.True(repo.Refs["refs/tags/lw"].IsTag());
            }

            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.True(repo.Refs["refs/notes/commits"].IsNote());
            }
        }

        [Fact]
        public void CanQueryReachability()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
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
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                var result = repo.Refs.ReachableFrom(
                    repo.Refs.Where(r => r.IsTag()),
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
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.ReachableFrom(null));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.ReachableFrom(null, repo.Commits.Take(2)));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.ReachableFrom(repo.Refs, null));
                Assert.Empty(repo.Refs.ReachableFrom(new Commit[] { }));
            }
        }
    }
}
