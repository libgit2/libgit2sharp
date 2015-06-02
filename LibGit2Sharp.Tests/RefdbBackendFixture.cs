using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefdbBackendFixture : BaseFixture
    {
        [Fact]
        public void CanWriteToRefdbBackend()
        {
            string path = SandboxStandardTestRepo();

            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), true);

                Assert.Equal(backend.References["refs/heads/newref"], new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644")));
            }
        }

        [Fact]
        public void CanReadFromRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Assert.True(repository.Refs["HEAD"].TargetIdentifier.Equals("refs/heads/testref"));
                Assert.True(repository.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier.Equals("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Branch branch = repository.Head;

                Assert.True(branch.CanonicalName.Equals("refs/heads/testref"));
            }
        }

        [Fact]
        public void CanDeleteFromRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                repository.Refs.Remove("refs/heads/testref");

                Assert.True(!backend.References.ContainsKey("refs/heads/testref"));
            }
        }

        [Fact]
        public void CannotOverwriteExistingDirectReferenceInRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repository = new Repository(path))
            {
                SetupBackend(repository);

                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);

                Assert.Throws<NameConflictException>(() => repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false));
            }
        }

        [Fact]
        public void CannotOverwriteExistingSymbolicReferenceInRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repository = new Repository(path))
            {
                SetupBackend(repository);

                repository.Refs.Add("refs/heads/directRef", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);
                repository.Refs.Add("refs/heads/directRef2", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);
                repository.Refs.Add("refs/heads/newref", "refs/heads/directRef", false);

                Assert.Throws<NameConflictException>(() => repository.Refs.Add("refs/heads/newref", "refs/heads/directRef2", false));
            }
        }

        [Fact]
        public void CanForcefullyOverwriteExistingDirectReferenceInRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repository = new Repository(path))
            {
                SetupBackend(repository);

                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);
                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), true);
            }
        }

        [Fact]
        public void CanForcefullyOverwriteExistingSymbolicReferenceInRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repository = new Repository(path))
            {
                SetupBackend(repository);

                repository.Refs.Add("refs/heads/directRef", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);
                repository.Refs.Add("refs/heads/directRef2", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);
                repository.Refs.Add("refs/heads/newref", "refs/heads/directRef", false);

                repository.Refs.Add("refs/heads/newref", "refs/heads/directRef2", true);

                var symRef = repository.Refs["refs/heads/newref"];
                Assert.Equal("refs/heads/directRef2", symRef.TargetIdentifier);
            }
        }

        [Fact]
        public void CanIterateRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");

                Assert.True(repository.Refs.Select(r => r.CanonicalName).SequenceEqual(backend.References.Keys));
            }
        }

        [Fact]
        public void CanIterateBrokenTypesInRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["refs/tags/broken1"] = new MockRefdbReference("tags/shouldnt/be/symbolic");
                backend.References["refs/tags/broken2"] = new MockRefdbReference("but/are/here/for/testing");
                backend.References["refs/tags/broken3"] = new MockRefdbReference("the/type/filtering");
                backend.References["refs/tags/correct1"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                List<string> tags = repository.Tags.Select(r => r.CanonicalName).ToList();
                Assert.True(tags.SequenceEqual(new List<string> { "refs/tags/correct1" }));
            }
        }

        [Fact]
        public void CanIterateTypesInRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/tags/correct"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                IEnumerable<string> tags = repository.Tags.Select(r => r.CanonicalName);
                Assert.True(tags.SequenceEqual(new List<string> { "refs/tags/correct" }));

                IEnumerable<string> branches = repository.Branches.Select(r => r.CanonicalName);
                Assert.True(branches.SequenceEqual(new List<string> { "refs/heads/othersymbolic", "refs/heads/testref" }));
            }
        }

        [Fact]
        public void CanIterateRefdbBackendWithGlob()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");

                Assert.Equal(repository.Refs.FromGlob("refs/heads/*").Select(r => r.CanonicalName), new string[] { "refs/heads/othersymbolic", "refs/heads/testref" });
                Assert.Equal(repository.Refs.FromGlob("refs/heads/?estref").Select(r => r.CanonicalName), new string[] { "refs/heads/testref" });
            }
        }

        [Fact]
        public void CanRenameFromRefDbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string originalRefName = "refs/heads/testref";
            string renamedRefName = "refs/heads/testref2";

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References[originalRefName] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Reference myRef = repository.Refs[originalRefName];
                Reference renamedRef = repository.Refs.Rename(myRef, renamedRefName);

                // original ref name should not be found
                Assert.Null(repository.Refs[originalRefName]);
                Assert.NotNull(repository.Refs[renamedRefName]);
            }
        }

        [Fact]
        public void CannotRenameOverExistingRef()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string originalRefName = "refs/heads/testref";
            string renamedRefName = "refs/heads/testref2";

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References[originalRefName] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References[renamedRefName] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Reference myRef = repository.Refs[originalRefName];
                Assert.Throws<NameConflictException>(() =>
                    repository.Refs.Rename(myRef, renamedRefName));

                // original ref name should  be found
                Assert.NotNull(repository.Refs[originalRefName]);
            }
        }

        [Fact]
        public void CanForcefullyRenameOverExistingRef()
        {
            // TODO: test with a different target ref

            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string originalRefName = "refs/heads/testref";
            string renamedRefName = "refs/heads/testref2";

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References[originalRefName] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References[renamedRefName] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Reference myRef = repository.Refs[originalRefName];
                Reference renamedRef = repository.Refs.Rename(myRef, renamedRefName, true);

                // original ref name should not be found
                Assert.Null(repository.Refs[originalRefName]);
                Assert.NotNull(repository.Refs[renamedRefName]);
            }
        }

        [Fact]
        public void RenamingNonexistentRefThrows()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string originalRefName = "refs/heads/testref";
            string renamedRefName = "refs/heads/testref2";

            using (Repository repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References[originalRefName] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Reference myRef = repository.Refs[originalRefName];

                repository.Refs.Remove(myRef);

                Assert.Throws<NotFoundException>(() =>
                    repository.Refs.Rename(myRef, renamedRefName));
            }
        }

        [Fact]
        public void CanCompressFromRefDbBackend()
        {
        }

        [Fact]
        public void CanLockAndUnlockFromRefDbBackend()
        {

        }

        #region Shared transaction tests

        public static ObjectId oid1 = new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
        public static ObjectId oid2 = new ObjectId("580c2111be43802dab11328176d94c391f1deae9");

        [Fact]
        public void ReferenceIsNotRemovedWhenTransactionIsNotCommittedRefDb()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string refName = "refs/heads/myref";

            using (var repo = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repo);
                backend.References[refName] = new MockRefdbReference(oid1);

                var myRef = repo.Refs[refName];

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    tx.RemoveReference(myRef);
                }

                Assert.NotNull(repo.Refs[myRef.CanonicalName]);
            }
        }

        [Fact]
        public void CanRemoveReferenceInTransactionRefDb()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string refName = "refs/heads/myref";

            using (var repo = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repo);
                backend.References[refName] = new MockRefdbReference(oid1);

                var myRef = repo.Refs[refName];

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    tx.RemoveReference(myRef);
                    tx.Commit();
                }

                Assert.Null(repo.Refs[myRef.CanonicalName]);
            }
        }

        [Fact]
        public void CanUpdateDirectReferenceInTransactionRefDb()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string refName = "refs/heads/myref";

            using (var repo = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repo);

                backend.References[refName] = new MockRefdbReference(oid1);

                var myRef = repo.Refs[refName];

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    tx.UpdateTarget(myRef, oid2, "updated by me");
                    tx.Commit();
                }

                var updatedRef = repo.Refs[refName];
                Assert.NotNull(updatedRef);
                Assert.Equal(updatedRef.TargetIdentifier, oid2.Sha);
            }
        }

        [Fact]
        public void CanUpdateSymbolicReferenceInTransactionRefDb()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string mySymRefName = "refs/heads/symRef";
            string refTargetName = "refs/heads/myref";
            string refTarget2Name = "refs/heads/myref2";

            using (var repo = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repo);

                var refTarget1 = repo.Refs.Add(refTargetName, oid1);
                var refTarget2 = repo.Refs.Add(refTarget2Name, oid2);
                var mySymRef = repo.Refs.Add(mySymRefName, refTargetName, null, true);

                using (var tx = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(mySymRef);
                    tx.UpdateTarget(mySymRef, refTarget2, null);
                    tx.Commit();
                }

                var updatedRef = repo.Refs[mySymRefName];
                Assert.NotNull(updatedRef);
                Assert.Equal(updatedRef.TargetIdentifier, refTarget2Name);
            }
        }

        [Fact]
        public void LockingAlreadyLockedReferenceThrowsRefDb()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.RootedDirectoryPath);

            string myRefName = "refs/heads/myref";

            using (var repo = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repo);
                var myRef = repo.Refs.Add(myRefName, oid1);

                using (var tx = repo.Refs.NewRefTransaction())
                using (var tx2 = repo.Refs.NewRefTransaction())
                {
                    tx.LockReference(myRef);
                    Assert.Throws<LibGit2SharpException>(() => tx2.LockReference(myRef));
                }
            }
        }

        #endregion

        #region MockRefdbBackend

        /// <summary>
        ///  Kind type of a <see cref="MockRefdbReference"/>
        /// </summary>
        private enum ReferenceType
        {
            /// <summary>
            ///  A direct reference, the target is an object ID.
            /// </summary>
            Oid = 1,

            /// <summary>
            ///  A symbolic reference, the target is another reference.
            /// </summary>
            Symbolic = 2,
        }

        private class MockRefdbReference
        {
            public MockRefdbReference(string target)
            {
                Type = ReferenceType.Symbolic;
                Symbolic = target;
            }

            public MockRefdbReference(ObjectId target)
            {
                Type = ReferenceType.Oid;
                Oid = target;
            }

            public ReferenceType Type
            {
                get;
                private set;
            }

            public bool IsLocked { get; set; }

            public ObjectId Oid
            {
                get;
                private set;
            }

            public string Symbolic
            {
                get;
                private set;
            }

            public override int GetHashCode()
            {
                int result = 17;

                result = 37 * result + (int)Type;

                if (Type == ReferenceType.Symbolic)
                {
                    result = 37 * result + Symbolic.GetHashCode();
                }
                else
                {
                    result = 37 * result + Oid.GetHashCode();
                }

                return result;
            }

            public override bool Equals(object obj)
            {
                var other = obj as MockRefdbReference;

                if (other == null || Type != other.Type)
                {
                    return false;
                }

                if (Type == ReferenceType.Symbolic)
                {
                    return Symbolic.Equals(other.Symbolic);
                }

                return Oid.Equals(other.Oid);
            }
        }

        private class MockRefdbBackend : RefdbBackend
        {
            private readonly Repository repository;

            private readonly SortedDictionary<string, MockRefdbReference> references =
                new SortedDictionary<string, MockRefdbReference>();

            public MockRefdbBackend(Repository repository)
            {
                this.repository = repository;
            }

            protected override Repository Repository
            {
                get { return repository; }
            }

            public SortedDictionary<string, MockRefdbReference> References
            {
                get { return references; }
            }

            public bool Compressed { get; private set; }

            protected override RefdbBackendOperations SupportedOperations
            {
                get
                {
                    return RefdbBackendOperations.Compress;
                }
            }

            public override bool Exists(string referenceName)
            {
                return references.ContainsKey(referenceName);
            }

            public override bool Lookup(string referenceName, out bool isSymbolic, out ObjectId oid, out string symbolic)
            {
                MockRefdbReference reference;
                
                if (!references.TryGetValue(referenceName, out reference))
                {
                    isSymbolic = false;
                    oid = null;
                    symbolic = null;
                    return false;
                }

                isSymbolic = reference.Type == ReferenceType.Symbolic;
                oid = reference.Oid;
                symbolic = reference.Symbolic;
                return true;
            }

            public override void WriteDirectReference(string referenceCanonicalName, ObjectId target, bool force)
            {
                var storage = new MockRefdbReference(target);
                if (references.ContainsKey(referenceCanonicalName) && !force)
                {
                    throw new NameConflictException("A reference with this name already exists.");
                }

                references[referenceCanonicalName] = storage;
            }

            public override void RenameReference(string referenceName, string newReferenceName, bool force,
                out bool isSymbolic, out ObjectId oid, out string symbolic)
            {
                if (references.ContainsKey(referenceName))
                {

                    if (references.ContainsKey(newReferenceName) && !force)
                    {
                        throw new NameConflictException("error");
                    }

                    var refToRename = references[referenceName];
                    references[newReferenceName] = refToRename;
                    references.Remove(referenceName);

                    isSymbolic = refToRename.Type == ReferenceType.Symbolic;
                    oid = refToRename.Oid;
                    symbolic = refToRename.Symbolic;
                }
                else
                {
                    throw new Exception("error");
                }
            }

            public override void WriteSymbolicReference(string referenceCanonicalName, string targetCanonicalName, bool force)
            {
                var storage = new MockRefdbReference(targetCanonicalName);
                if (references.ContainsKey(referenceCanonicalName) && !force)
                {
                    throw new NameConflictException("A reference with this name already exists.");
                }

                references[referenceCanonicalName] = storage;
            }

            public override void Delete(string referenceCanonicalName)
            {
                references.Remove(referenceCanonicalName);
            }

            public override void Compress()
            {
                Compressed = true;
            }

            public override void Free()
            {
                references.Clear();
            }

            public override RefdbIterator GenerateRefIterator(string glob)
            {
                return new MockRefDbIterator(References, glob);
            }

            public override bool HasReflog(string refName)
            {
                return false;
            }

            public override void EnsureReflog(string refName)
            {
                
            }

            public override void ReadReflog()
            {
            }

            public override void WriteReflog()
            {
                throw new NotImplementedException();
            }

            public override void RenameReflog(string oldName, string newName)
            {
                throw new NotImplementedException();
            }

            public override void LockReference(string refName)
            {
                MockRefdbReference reference;

                if (!this.references.TryGetValue(refName, out reference))
                {
                    throw new LibGit2Sharp.NotFoundException(
                        string.Format("Reference {0} was not found.", refName));
                }

                if (reference.IsLocked)
                {
                    throw new LibGit2Sharp.LibGit2SharpException(
                        string.Format("Reference {0} is already locked.", refName));
                }

                reference.IsLocked = true;
            }

            public override void UnlockReference(string refName)
            {
                MockRefdbReference reference;

                if (!this.references.TryGetValue(refName, out reference))
                {
                    throw new LibGit2Sharp.NotFoundException(
                        string.Format("Reference {0} was not found.", refName));
                }

                reference.IsLocked = false;
            }
        }

        private class MockRefDbIterator : RefdbIterator
        {
            IDictionary<string, MockRefdbReference> references;
            IEnumerator<KeyValuePair<string, MockRefdbReference>> nextIterator;

            public MockRefDbIterator(IDictionary<string, MockRefdbReference> allRefs, string glob)
            {
                if (!string.IsNullOrEmpty(glob))
                {
                    Regex globRegex = new Regex("^" +
                        Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") +
                        "$");

                    references = allRefs.Where(kvp => globRegex.IsMatch(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
                else
                {
                    references = new Dictionary<string, MockRefdbReference>(allRefs);
                }

                nextIterator = references.GetEnumerator();
            }

            public override bool Next(out string referenceName, out bool isSymbolic, out ObjectId oid, out string symbolic)
            {
                if (!nextIterator.MoveNext())
                {
                    referenceName = null;
                    isSymbolic = false;
                    oid = null;
                    symbolic = null;
                    return false;
                }

                KeyValuePair<string, MockRefdbReference> next = nextIterator.Current;
                referenceName = next.Key;
                isSymbolic = next.Value.Type == ReferenceType.Symbolic;
                oid = next.Value.Oid;
                symbolic = next.Value.Symbolic;
                return true;
            }

            public override string NextName()
            {
                if (nextIterator.MoveNext())
                {
                    return nextIterator.Current.Key;
                }

                return null;
            }
        }

        #endregion

        private static MockRefdbBackend SetupBackend(Repository repository)
        {
            var backend = new MockRefdbBackend(repository);
            repository.Refs.SetBackend(backend);

            return backend;
        }
    }
}
