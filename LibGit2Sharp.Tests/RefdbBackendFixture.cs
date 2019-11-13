using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefdbBackendFixture : BaseFixture
    {
        [Fact]
        public void CanWriteToRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);
                repo.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), true);
                Assert.Equal(backend.Refs["refs/heads/newref"], new RefdbBackend.ReferenceData("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644")));
            }
        }

        [Fact]
        public void CanReadFromRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);
                backend.Refs["HEAD"] = new RefdbBackend.ReferenceData("HEAD", "refs/heads/testref");
                backend.Refs["refs/heads/testref"] = new RefdbBackend.ReferenceData("refs/heads/testref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Assert.Equal("refs/heads/testref", repo.Refs["HEAD"].TargetIdentifier);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", repo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.Equal("refs/heads/testref", repo.Head.CanonicalName);
            }
        }

        [Fact]
        public void CanDeleteFromRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (Repository repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);
                backend.Refs["HEAD"] = new RefdbBackend.ReferenceData("HEAD", "refs/heads/testref");
                backend.Refs["refs/heads/testref"] = new RefdbBackend.ReferenceData("refs/heads/testref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                repo.Refs.Remove("refs/heads/testref");

                Assert.True(!backend.Refs.ContainsKey("refs/heads/testref"));
            }
        }

        [Fact]
        public void CannotOverwriteExistingInRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (Repository repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);

                repo.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);

                Assert.Throws<NameConflictException>(() => repo.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false));

                // With allowOverwrite, it should succeed:
                repo.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), true);
            }
        }

        [Fact]
        public void CanIterateRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (Repository repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);

                backend.Refs["HEAD"] = new RefdbBackend.ReferenceData("HEAD", "refs/heads/testref");
                backend.Refs["refs/heads/testref"] = new RefdbBackend.ReferenceData("refs/heads/testref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.Refs["refs/heads/othersymbolic"] = new RefdbBackend.ReferenceData("refs/heads/othersymbolic", "refs/heads/testref");

                Assert.True(repo.Refs.Select(r => r.CanonicalName).SequenceEqual(backend.Refs.Keys));
            }
        }

        [Fact]
        public void CanIterateTagsInRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (Repository repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);

                // The behavior of libgit2 has changed:
                // If libgit2 can't resolve any tag to an OID, then git_tag_list silently fails and returns zero tags.
                // This test previously used broken refs to test type filtering, but refdb is no longer responsible for type filtering.
                // The old test code is commented below:
                // backend.Refs["refs/tags/broken1"] = new RefdbBackend.ReferenceData("refs/tags/broken1", "tags/shouldnt/be/symbolic");
                // backend.Refs["refs/tags/broken2"] = new RefdbBackend.ReferenceData("refs/tags/broken2", "but/are/here/for/testing");
                // backend.Refs["refs/tags/broken3"] = new RefdbBackend.ReferenceData("refs/tags/broken3", "the/type/filtering");

                backend.Refs["refs/tags/correct1"] = new RefdbBackend.ReferenceData("refs/tags/correct1", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                var tagNames = repo.Tags.Select(r => r.CanonicalName);
                Assert.True(tagNames.SequenceEqual(new List<string> { "refs/tags/correct1" }));
            }
        }

        [Fact]
        public void CanIterateRefdbBackendWithGlob()
        {
            string path = SandboxStandardTestRepo();
            using (Repository repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);

                backend.Refs["HEAD"] = new RefdbBackend.ReferenceData("HEAD", "refs/heads/testref");
                backend.Refs["refs/heads/testref"] = new RefdbBackend.ReferenceData("refs/heads/testref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.Refs["refs/heads/othersymbolic"] = new RefdbBackend.ReferenceData("refs/heads/othersymbolic", "refs/heads/testref");

                Assert.True(repo.Refs.FromGlob("refs/heads/*").Select(r => r.CanonicalName).SequenceEqual(new List<string>() { "refs/heads/othersymbolic", "refs/heads/testref" }));
                Assert.True(repo.Refs.FromGlob("refs/heads/?estref").Select(r => r.CanonicalName).SequenceEqual(new List<string>() { "refs/heads/testref" }));
            }
        }

        [Fact]
        public void RefdbBackendCanRenameAReferenceToADeeperReferenceHierarchy()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var backend = new MockRefdbBackend(repo);
                repo.Refs.SetBackend(backend);
                backend.Refs["refs/tags/test"] = new RefdbBackend.ReferenceData("refs/tags/test", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                const string newName = "refs/tags/test/deep";

                Reference renamed = repo.Refs.Rename("refs/tags/test", newName);
                Assert.NotNull(renamed);
                Assert.Equal(newName, renamed.CanonicalName);
            }
        }

        private class MockRefdbBackend : RefdbBackend
        {
            public MockRefdbBackend(Repository repository) : base(repository)
            {
            }

            public SortedDictionary<string, ReferenceData> Refs { get; } = new SortedDictionary<string, ReferenceData>();

            public override bool Exists(string refName)
            {
                return Refs.ContainsKey(refName);
            }

            public override IEnumerable<ReferenceData> Iterate(string glob)
            {
                if (string.IsNullOrEmpty(glob))
                {
                    return Refs.Values;
                }
                else
                {
                    var globRegex = new Regex("^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$");
                    return Refs.Values.Where(r => globRegex.IsMatch(r.RefName));
                }
            }

            public override bool Lookup(string refName, out ReferenceData data)
            {
                return Refs.TryGetValue(refName, out data);
            }

            public override void Delete(ReferenceData refData)
            {
                if (!this.Refs.Remove(refData.RefName))
                {
                    throw RefdbBackendException.NotFound(refData.RefName);
                }
            }

            public override void Write(ReferenceData newRef, ReferenceData oldRef, bool force, Signature signature, string message)
            {
                ReferenceData existingRef;
                if (!force && this.Refs.TryGetValue(newRef.RefName, out existingRef))
                {
                    // If either oldRef wasn't provided/didn't match, or force isn't enabled, reject.
                    if ((oldRef != null && !existingRef.Equals(oldRef)))
                    {
                        throw RefdbBackendException.Conflict(newRef.RefName);
                    }

                    throw RefdbBackendException.Exists(newRef.RefName);
                }

                this.Refs[newRef.RefName] = newRef;
            }

            public override ReferenceData Rename(string oldName, string newName, bool force, Signature signature, string message)
            {
                ReferenceData oldValue;
                if (!this.Refs.TryGetValue(oldName, out oldValue))
                {
                    throw RefdbBackendException.NotFound(oldName);
                }

                if (!force && this.Refs.ContainsKey(newName))
                {
                    throw RefdbBackendException.Exists(newName);
                }

                ReferenceData newRef;
                if (oldValue.IsSymbolic)
                {
                    newRef = new ReferenceData(newName, oldValue.SymbolicTarget);
                }
                else
                {
                    newRef = new ReferenceData(newName, oldValue.ObjectId);
                }

                this.Refs.Remove(oldName);
                this.Refs[newName] = newRef;
                return newRef;
            }
        }
    }
}
