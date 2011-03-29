#region  Copyright (c) 2011 LibGit2Sharp committers

//  The MIT License
//  
//  Copyright (c) 2011 LibGit2Sharp committers
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ReferenceFixture : ReadWriteRepositoryFixtureBase
    {
        private readonly List<string> expectedRefs = new List<string> {"refs/heads/packed-test", "refs/heads/packed", "refs/heads/br2", "refs/heads/master", "refs/heads/test", "refs/tags/test", "refs/tags/very-simple"};

        [Test]
        public void CanCreateReferenceFromSha()
        {
            const string name = "refs/heads/unit_test";
            using (var repo = new Repository(PathToReadWriteRepository))
            {
                var newRef = repo.Refs.Create(name, "be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                newRef.ShouldNotBeNull();
                newRef.Name.ShouldEqual(name);
                newRef.Target.ShouldNotBeNull();
                newRef.Target.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                repo.Refs.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();

                newRef.Delete();
            }
        }

        [Test]
        public void CanCreateReferenceFromSymbol()
        {
            const string name = "refs/heads/unit_test";
            using (var repo = new Repository(PathToReadWriteRepository))
            {
                var newRef = repo.Refs.Create(name, "refs/heads/master");
                newRef.ShouldNotBeNull();
                newRef.Name.ShouldEqual(name);
                newRef.Target.ShouldNotBeNull();
                newRef.Target.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                repo.Refs.SingleOrDefault(p => p.Name == name).ShouldNotBeNull();

                newRef.Delete();
            }
        }

        [Test]
        public void CanListAllReferences()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                foreach (var r in repo.Refs)
                {
                    Assert.Contains(r.Name, expectedRefs);
                }

                repo.Refs.Count().ShouldEqual(7);
            }
        }

        [Test]
        public void CanResolveHeadByName()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var head = repo.Refs["HEAD"];
                head.ShouldNotBeNull();
                // todo: unsure about this strategy of always peeling back refs
                //head.Name.ShouldEqual("refs/heads/master");
                head.Name.ShouldEqual("HEAD");
                head.Target.ShouldNotBeNull();
                head.Target.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                Assert.IsInstanceOf<Commit>(head.Target);

                var head2 = repo.Refs.Head();
                //head2.Name.ShouldEqual("refs/heads/master");
                head2.Name.ShouldEqual("HEAD");
                head2.Target.ShouldNotBeNull();
                Assert.IsInstanceOf<Commit>(head2.Target);

                head.Equals(head2).ShouldBeTrue();
            }
        }

        [Test]
        public void CanResolveRefsByName()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var master = repo.Refs["refs/heads/master"];
                master.ShouldNotBeNull();
                master.Name.ShouldEqual("refs/heads/master");
                master.Target.ShouldNotBeNull();
                master.Target.Sha.ShouldEqual("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                Assert.IsInstanceOf<Commit>(master.Target);
            }
        }

        [Test]
        public void CreateWithEmptyStringForTargetThrows()
        {
            using (var repo = new Repository(PathToReadWriteRepository))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Create("refs/heads/newref", string.Empty));
            }
        }

        [Test]
        public void CreateWithEmptyStringThrows()
        {
            using (var repo = new Repository(PathToReadWriteRepository))
            {
                Assert.Throws<ArgumentException>(() => repo.Refs.Create(string.Empty, "refs/heads/master"));
            }
        }

        [Test]
        public void CreateWithNullForTargetThrows()
        {
            using (var repo = new Repository(PathToReadWriteRepository))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Refs.Create("refs/heads/newref", null));
            }
        }

        [Test]
        public void CreateWithNullStringThrows()
        {
            using (var repo = new Repository(PathToReadWriteRepository))
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