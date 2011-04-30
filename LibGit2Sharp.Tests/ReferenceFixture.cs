using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ReferenceFixture
    {
        private readonly List<string> expectedRefs = new List<string> {"refs/heads/packed-test", "refs/heads/packed", "refs/heads/br2", "refs/heads/master", "refs/heads/test", "refs/tags/test", "refs/tags/e90810b", "refs/tags/lw"};

        [Test]
        public void CanCreateADirectReference()
        {
            const string name = "refs/heads/unit_test";

            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newRef = (DirectReference) repo.Refs.Create(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                newRef.ShouldNotBeNull();
                newRef.CanonicalName.ShouldEqual(name);
                newRef.Target.ShouldNotBeNull();
                newRef.Target.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                repo.Refs[name].ShouldNotBeNull();
            }
        }

        [Test]
        public void CanCreateASymbolicReference()
        {
            const string name = "refs/heads/unit_test";
            const string target = "refs/heads/master";

            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newRef = (SymbolicReference) repo.Refs.Create(name, target);
                newRef.ShouldNotBeNull();
                newRef.CanonicalName.ShouldEqual(name);
                newRef.Target.CanonicalName.ShouldEqual(target);
                newRef.ResolveToDirectReference().Target.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                repo.Refs[name].ShouldNotBeNull();
            }
        }

        [Test]
        public void BlindlyCreatingADirectReferenceOverAnExistingOneThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Refs.Create("refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
            }
        }

        [Test]
        public void BlindlyCreatingASymbolicReferenceOverAnExistingOneThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Refs.Create("HEAD", "refs/head/br2"));
            }
        }

        [Test]
        public void CanCreateAndOverwriteADirectReference()
        {
            const string name = "refs/heads/br2";
            const string target = "4c062a6361ae6959e06292c1fa5e2822d9c96345";

            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newRef = (DirectReference)repo.Refs.Create(name, target, true);
                newRef.ShouldNotBeNull();
                newRef.CanonicalName.ShouldEqual(name);
                newRef.Target.ShouldNotBeNull();
                newRef.Target.Sha.ShouldEqual(target);
                ((DirectReference)repo.Refs[name]).Target.Sha.ShouldEqual(target);
            }
        }

        [Test]
        public void CanCreateAndOverwriteASymbolicReference()
        {
            const string name = "HEAD";
            const string target = "refs/heads/br2";

            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newRef = (SymbolicReference)repo.Refs.Create(name, target, true);
                newRef.ShouldNotBeNull();
                newRef.CanonicalName.ShouldEqual(name);
                newRef.Target.ShouldNotBeNull();
                newRef.ResolveToDirectReference().Target.Sha.ShouldEqual("a4a7dce85cf63874e984719f4fdd239f5145052f");
                ((SymbolicReference)repo.Head).Target.CanonicalName.ShouldEqual(target);
            }
        }

        [Test]
        public void CreateWithEmptyStringForTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Create("refs/heads/newref", string.Empty));
            }
        }

        [Test]
        public void CreateWithEmptyStringThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Create(string.Empty, "refs/heads/master"));
            }
        }

        [Test]
        public void CreateWithNullForTargetThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Create("refs/heads/newref", null));
            }
        }

        [Test]
        public void CreateWithNullStringThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Create(null, "refs/heads/master"));
            }
        }

        [Test]
        public void CanDeleteAReference()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Refs.Delete("refs/heads/packed");
            }
        }

        [Test]
        public void ADeletedReferenceCannotBeLookedUp()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string refName = "refs/heads/test";

                repo.Refs.Delete(refName);
                repo.Refs[refName].ShouldBeNull();
            }
        }

        [Test]
        public void DeletingAReferenceDecreasesTheRefsCount()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string refName = "refs/heads/test";

                var refs = repo.Refs.Select(r => r.CanonicalName).ToList();
                refs.Contains(refName).ShouldBeTrue();

                repo.Refs.Delete(refName);

                var refs2 = repo.Refs.Select(r => r.CanonicalName).ToList();
                refs2.Contains(refName).ShouldBeFalse();

                refs2.Count.ShouldEqual(refs.Count - 1);
            }
        }

        [Test]
        public void DeleteWithEmptyNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Delete(string.Empty));
            }
        }

        [Test]
        public void DeleteWithNullNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Delete(null));
            }
        }

        [Test]
        public void CanListAllReferences()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var r in repo.Refs)
                {
                    Assert.Contains(r.CanonicalName, expectedRefs);
                }

                repo.Refs.Count().ShouldEqual(8);
            }
        }

        [Test]
        public void CanResolveHeadByName()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var head = (SymbolicReference) repo.Refs["HEAD"];
                head.ShouldNotBeNull();
                head.CanonicalName.ShouldEqual("HEAD");
                head.Target.ShouldNotBeNull();
                head.Target.CanonicalName.ShouldEqual("refs/heads/master");
                ((DirectReference) head.Target).Target.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                Assert.IsInstanceOf<Commit>(((DirectReference) head.Target).Target);

                var head2 = (SymbolicReference) repo.Head;
                head2.CanonicalName.ShouldEqual("HEAD");
                head2.Target.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(((DirectReference) head.Target).Target);

                head.Equals(head2).ShouldBeTrue();
            }
        }

        [Test]
        public void CanResolveReferenceToALightweightTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var lwTag = (DirectReference) repo.Refs["refs/tags/lw"];
                lwTag.ShouldNotBeNull();
                lwTag.CanonicalName.ShouldEqual("refs/tags/lw");
                lwTag.Target.ShouldNotBeNull();
                lwTag.Target.Sha.ShouldEqual("e90810b8df3e80c413d903f631643c716887138d");
                Assert.IsInstanceOf<Commit>(lwTag.Target);
            }
        }

        [Test]
        public void CanResolveReferenceToAnAnnotatedTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var annTag = (DirectReference) repo.Refs["refs/tags/test"];
                annTag.ShouldNotBeNull();
                annTag.CanonicalName.ShouldEqual("refs/tags/test");
                annTag.Target.ShouldNotBeNull();
                annTag.Target.Sha.ShouldEqual("b25fa35b38051e4ae45d4222e795f9df2e43f1d1");
                Assert.IsInstanceOf<TagAnnotation>(annTag.Target);
            }
        }

        [Test]
        public void CanResolveRefsByName()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var master = (DirectReference) repo.Refs["refs/heads/master"];
                master.ShouldNotBeNull();
                master.CanonicalName.ShouldEqual("refs/heads/master");
                master.Target.ShouldNotBeNull();
                master.Target.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                Assert.IsInstanceOf<Commit>(master.Target);
            }
        }

        [Test]
        public void ResolvingWithEmptyStringThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => { var head = repo.Refs[string.Empty]; });
            }
        }

        [Test]
        public void ResolvingWithNullThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => { var head = repo.Refs[null]; });
            }
        }

        [Test]
        public void CanUpdateTargetOnReference()
        {
            const string masterRef = "refs/heads/master";
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var sha = repo.Refs["refs/heads/test"].ResolveToDirectReference().Target.Sha;
                var master = repo.Refs[masterRef];
                master.ResolveToDirectReference().Target.Sha.ShouldNotEqual(sha);

                repo.Refs.UpdateTarget(masterRef, sha);

                master = repo.Refs[masterRef];
                master.ResolveToDirectReference().Target.Sha.ShouldEqual(sha);
            }
        }

        [Test]
        public void CanUpdateTargetOnSymbolicReference()
        {
            const string name = "refs/heads/unit_test";
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newRef = (SymbolicReference) repo.Refs.Create(name, "refs/heads/master");
                newRef.ShouldNotBeNull();

                repo.Refs.UpdateTarget(newRef.CanonicalName, "refs/heads/test");

                newRef = (SymbolicReference) repo.Refs[newRef.CanonicalName];
                newRef.ResolveToDirectReference().Target.ShouldEqual(repo.Refs["refs/heads/test"].ResolveToDirectReference().Target);

                repo.Refs.Delete(newRef.CanonicalName);
            }
        }

        [Test]
        public void UpdatingADirectRefWithSymbolFails()
        {
            const string name = "refs/heads/unit_test";
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newRef = (SymbolicReference) repo.Refs.Create(name, "refs/heads/master");
                newRef.ShouldNotBeNull();

                Assert.Throws<ArgumentException>(
                    () => repo.Refs.UpdateTarget(newRef.CanonicalName, repo.Refs["refs/heads/test"].ResolveToDirectReference().Target.Sha));

                repo.Refs.Delete(newRef.CanonicalName);
            }
        }

        [Test]
        public void UpdatingASymbolicRefWithOidFails()
        {
            const string masterRef = "refs/heads/master";
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.UpdateTarget(masterRef, "refs/heads/test"));
            }
        }

        [Test]
        public void UpdatingAReferenceTargetWithBadParametersFails()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.UpdateTarget(string.Empty, "refs/heads/packed"));
                Assert.Throws<ArgumentException>(() => repo.Refs.UpdateTarget("master", string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.UpdateTarget(null, "refs/heads/packed"));
                Assert.Throws<ArgumentNullException>(() => repo.Refs.UpdateTarget("master", null));
            }
        }

        [Test]
        public void CanMoveAReference()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string newName = "refs/atic/tagtest";

                Reference moved = repo.Refs.Move("refs/tags/test", newName);
                moved.ShouldNotBeNull();
                moved.CanonicalName.ShouldEqual(newName);
            }
        }

        [Test]
        public void MovingANonExistingReferenceThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Refs.Move("refs/tags/i-am-void", "refs/atic/tagtest"));
            }
        }

        [Test]
        public void CanMoveAndOverWriteAExistingReference()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string oldName = "refs/heads/packed";
                const string newName = "refs/heads/br2";
                
                Reference moved = repo.Refs.Move(oldName, newName, true);

                repo.Refs[oldName].ShouldBeNull();
                repo.Refs[moved.CanonicalName].ShouldNotBeNull();
            }
        }

        [Test]
        public void BlindlyOverwritingAExistingReferenceThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Refs.Move("refs/heads/packed", "refs/heads/br2"));
            }
        }

        [Test]
        public void MovingAReferenceDoesNotDecreaseTheRefsCount()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                var refs = repo.Refs.Select(r => r.CanonicalName).ToList();
                refs.Contains(oldName).ShouldBeTrue();

                repo.Refs.Move(oldName, newName);

                var refs2 = repo.Refs.Select(r => r.CanonicalName).ToList();
                refs2.Contains(oldName).ShouldBeFalse();
                refs2.Contains(newName).ShouldBeTrue();

                refs.Count.ShouldEqual(refs2.Count);
            }
        }

        [Test]
        public void CanLookupAMovedReference()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                const string oldName = "refs/tags/test";
                const string newName = "refs/atic/tagtest";

                Reference moved = repo.Refs.Move(oldName, newName);

                Reference lookedUp = repo.Refs[newName];
                moved.ShouldEqual(lookedUp);
            }
        }
    }
}