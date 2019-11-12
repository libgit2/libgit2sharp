using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefdbBackendFixture : BaseFixture
    {
        [Fact]
        public void CanReadFromRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            Repository.Init(scd.RootedDirectoryPath);
            using (var repo = new Repository(scd.RootedDirectoryPath))
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

            public override RefIterator Iterate(string glob)
            {
                return new MockRefIterator(this);
            }

            public override bool Lookup(string refName, out ReferenceData data)
            {
                return Refs.TryGetValue(refName, out data);
            }

            private class MockRefIterator : RefIterator
            {
                private readonly IEnumerator<ReferenceData> enumerator;

                public MockRefIterator(MockRefdbBackend parent)
                {
                    this.enumerator = parent.Refs.Values.GetEnumerator();
                }

                public override ReferenceData GetNext()
                {
                    if (this.enumerator.MoveNext())
                    {
                        return this.enumerator.Current;
                    }

                    return null;
                }
            }
        }
    }
}
