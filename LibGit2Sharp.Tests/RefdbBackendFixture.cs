using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefdbBackendFixture : BaseFixture
    {
        [Fact]
        public void CanWriteToRefdbBackend()
        {
            string path = CloneStandardTestRepo();
            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = new MockRefdbBackend(repository);
                repository.ReferenceDatabase.SetBackend(backend);

                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), true);

                Assert.Equal(backend.References["refs/heads/newref"], new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644")));
            }
        }

        [Fact]
        public void CanReadFromRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);

            using (Repository repository = Repository.Init(scd.RootedDirectoryPath))
            {
                MockRefdbBackend backend = new MockRefdbBackend(repository);
                repository.ReferenceDatabase.SetBackend(backend);

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

            using (Repository repository = Repository.Init(scd.RootedDirectoryPath))
            {
                MockRefdbBackend backend = new MockRefdbBackend(repository);
                repository.ReferenceDatabase.SetBackend(backend);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                repository.Refs.Remove("refs/heads/testref");

                Assert.True(!backend.References.ContainsKey("refs/heads/testref"));
            }
        }

        [Fact]
        public void CannotOverwriteExistingInRefdbBackend()
        {
            string path = CloneStandardTestRepo();
            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = new MockRefdbBackend(repository);
                repository.ReferenceDatabase.SetBackend(backend);

                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);

                Assert.Throws<NameConflictException>(() => repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false));
            }
        }

        [Fact]
        public void CanIterateRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);

            using (Repository repository = Repository.Init(scd.RootedDirectoryPath))
            {
                MockRefdbBackend backend = new MockRefdbBackend(repository);
                repository.ReferenceDatabase.SetBackend(backend);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");

                Assert.True(repository.Refs.Select(r => r.CanonicalName).SequenceEqual(backend.References.Keys));
            }
        }

        [Fact]
        public void CanIterateTypesInRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);

            using (Repository repository = Repository.Init(scd.RootedDirectoryPath))
            {
                MockRefdbBackend backend = new MockRefdbBackend(repository);
                repository.ReferenceDatabase.SetBackend(backend);

                backend.References["refs/tags/broken1"] = new MockRefdbReference("tags/shouldnt/be/symbolic");
                backend.References["refs/tags/broken2"] = new MockRefdbReference("but/are/here/for/testing");
                backend.References["refs/tags/broken3"] = new MockRefdbReference("the/type/filtering");
                backend.References["refs/tags/correct1"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Assert.True(repository.Tags.Select(r => r.CanonicalName).SequenceEqual(new List<string> { "refs/tags/correct1" }));
            }
        }

        [Fact]
        public void CanIterateRefdbBackendWithGlob()
        {
            var scd = new SelfCleaningDirectory(this);

            using (Repository repository = Repository.Init(scd.RootedDirectoryPath))
            {
                MockRefdbBackend backend = new MockRefdbBackend(repository);
                repository.ReferenceDatabase.SetBackend(backend);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");

                Assert.True(repository.Refs.FromGlob("refs/heads/*").Select(r => r.CanonicalName).SequenceEqual(new List<string>() { "refs/heads/othersymbolic", "refs/heads/testref" }));
                Assert.True(repository.Refs.FromGlob("refs/heads/?estref").Select(r => r.CanonicalName).SequenceEqual(new List<string>() { "refs/heads/testref" }));
            }
        }

        #region MockRefdbBackend

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
                MockRefdbReference other = obj as MockRefdbReference;

                if (other == null || Type != other.Type)
                {
                    return false;
                }

                if (Type == ReferenceType.Symbolic)
                {
                    return Symbolic.Equals(other.Symbolic);
                }
                else
                {
                    return Oid.Equals(other.Oid);
                }
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
                get
                {
                    return repository;
                }
            }

            public SortedDictionary<string, MockRefdbReference> References
            {
                get
                {
                    return references;
                }
            }

            public bool Compressed
            {
                get;
                private set;
            }

            protected override RefdbBackendOperations SupportedOperations
            {
                get
                {
                    return RefdbBackendOperations.Compress | RefdbBackendOperations.ForeachGlob;
                }
            }

            public override bool Exists(string referenceName)
            {
                return references.ContainsKey(referenceName);
            }

            public override bool Lookup(string referenceName, out ReferenceType type, out ObjectId oid, out string symbolic)
            {
                MockRefdbReference reference = references[referenceName];

                if (reference == null)
                {
                    type = ReferenceType.Invalid;
                    oid = null;
                    symbolic = null;
                    return false;
                }

                type = reference.Type;
                oid = reference.Oid;
                symbolic = reference.Symbolic;
                return true;
            }

            public override int Foreach(ReferenceType types, ForeachCallback callback)
            {
                int result = 0;

                foreach (KeyValuePair<string, MockRefdbReference> kvp in references)
                {
                    if ((types & kvp.Value.Type) == 0)
                    {
                        continue;
                    }

                    if ((result = callback(kvp.Key)) != 0)
                    {
                        return result;
                    }
                }

                return result;
            }

            public override int ForeachGlob(ReferenceType types, string glob, ForeachCallback callback)
            {
                int result = 0;

                Regex globRegex = new Regex("^" +
                    Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") +
                    "$");

                foreach (KeyValuePair<string, MockRefdbReference> kvp in references)
                {
                    if ((types & kvp.Value.Type) == 0)
                    {
                        continue;
                    }

                    if (!globRegex.IsMatch(kvp.Key))
                    {
                        continue;
                    }

                    if ((result = callback(kvp.Key)) != 0)
                    {
                        return result;
                    }
                }

                return result;
            }

            public override void Write(Reference reference)
            {
                MockRefdbReference storage;

                if (reference is SymbolicReference)
                {
                    storage = new MockRefdbReference(((SymbolicReference)reference).TargetIdentifier);
                }
                else
                {
                    storage = new MockRefdbReference(((DirectReference)reference).Target.Id);
                }

                references.Add(reference.CanonicalName, storage);
            }

            public override void Delete(Reference reference)
            {
                references.Remove(reference.CanonicalName);
            }

            public override void Compress()
            {
                Compressed = true;
            }

            public override void Free()
            {
                references.Clear();
            }
        }

        #endregion
    }
}
