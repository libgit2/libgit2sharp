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
        private readonly List<string> expectedRefs = new List<string> { "refs/heads/packed-test", "refs/heads/packed", "refs/heads/br2", "refs/heads/master", "refs/heads/test", "refs/tags/test", "refs/tags/e90810b", "refs/tags/lw" };

        [Test]
        public void CanCreateReferenceFromSha()
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
                repo.Refs.SingleOrDefault(p => p.CanonicalName == name).ShouldNotBeNull();

                repo.Refs.Delete(newRef.CanonicalName);
            }
        }

        [Test]
        public void CanCreateReferenceFromSymbol()
        {
            const string name = "refs/heads/unit_test";
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                var newRef = (SymbolicReference) repo.Refs.Create(name, "refs/heads/master");
                newRef.ShouldNotBeNull();
                newRef.CanonicalName.ShouldEqual(name);
                newRef.Target.ShouldNotBeNull();
                ((DirectReference) newRef.Target).Target.Sha.ShouldEqual("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                repo.Refs.SingleOrDefault(p => p.CanonicalName == name).ShouldNotBeNull();

                repo.Refs.Delete(newRef.CanonicalName);
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
        public void DeleteWithEmptyNameThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Delete(string.Empty));
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

                var head2 = (SymbolicReference) repo.Refs.Head;
                head2.CanonicalName.ShouldEqual("HEAD");
                head2.Target.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(((DirectReference) head.Target).Target);

                head.Equals(head2).ShouldBeTrue();
            }
        }

        [Test]
        public void CanResolveReferenceToAnAnnotatedTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var annTag = (DirectReference)repo.Refs["refs/tags/test"];
                annTag.ShouldNotBeNull();
                annTag.CanonicalName.ShouldEqual("refs/tags/test");
                annTag.Target.ShouldNotBeNull();
                annTag.Target.Sha.ShouldEqual("b25fa35b38051e4ae45d4222e795f9df2e43f1d1");
                Assert.IsInstanceOf<TagAnnotation>(annTag.Target);
            }
        }

        [Test]
        public void CanResolveReferenceToALightweightTag()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var lwTag = (DirectReference)repo.Refs["refs/tags/lw"];
                lwTag.ShouldNotBeNull();
                lwTag.CanonicalName.ShouldEqual("refs/tags/lw");
                lwTag.Target.ShouldNotBeNull();
                lwTag.Target.Sha.ShouldEqual("e90810b8df3e80c413d903f631643c716887138d");
                Assert.IsInstanceOf<Commit>(lwTag.Target);
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
        public void CreateWithEmptyStringForTargetThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Create("refs/heads/newref", string.Empty));
            }
        }

        [Test]
        public void CreateWithEmptyStringThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Create(string.Empty, "refs/heads/master"));
            }
        }

        [Test]
        public void CreateWithNullForTargetThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Create("refs/heads/newref", (string)null));
            }
        }

        [Test]
        public void CreateWithNullStringThrows()
        {
            using (var path = new TemporaryCloneOfTestRepo())
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Create(null, "refs/heads/master"));
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
    }
}