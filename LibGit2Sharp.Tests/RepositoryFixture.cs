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
using System.IO;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RepositoryFixture
    {
        private const string newRepoPath = "new_repo";

        [Test]
        public void CanTellIfObjectsExistInRepository()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Exists("8496071c1b46c854b31185ea97743be6a8774479").ShouldBeTrue();
                repo.Exists("1385f264afb75a56a5bec74243be9b367ba4ca08").ShouldBeTrue();
                repo.Exists("ce08fe4884650f067bd5703b6a59a8b3b3c99a09").ShouldBeFalse();
                repo.Exists("8496071c1c46c854b31185ea97743be6a8774479").ShouldBeFalse();
            }
        }

        private const string commitSha = "8496071c1b46c854b31185ea97743be6a8774479";
        private const string notFoundSha = "ce08fe4884650f067bd5703b6a59a8b3b3c99a09";

        [Test]
        public void CallingExistsWithEmptyThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Exists(string.Empty));
            }
        }

        [Test]
        public void CallingExistsWithNullThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Exists(null));
            }
        }

        [Test]
        public void CanCreateRepo()
        {
            using (new SelfCleaningDirectory(newRepoPath))
            using (new Repository(newRepoPath, new RepositoryOptions {CreateIfNeeded = true}))
            {
                Directory.Exists(newRepoPath).ShouldBeTrue();
            }
        }

        [Test]
        public void CanDisposeObjects()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                using (var commit = repo.Lookup(commitSha))
                {
                }
            }
        }

        [Test]
        public void CanLookupObjects()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup(commitSha).ShouldNotBeNull();
                repo.TryLookup(commitSha).ShouldNotBeNull();
                repo.Lookup<Commit>(commitSha).ShouldNotBeNull();
                repo.TryLookup<Commit>(commitSha).ShouldNotBeNull();
                repo.Lookup<GitObject>(commitSha).ShouldNotBeNull();
                repo.TryLookup<GitObject>(commitSha).ShouldNotBeNull();

                Assert.Throws<KeyNotFoundException>(() => repo.Lookup(notFoundSha));
                Assert.Throws<KeyNotFoundException>(() => repo.Lookup<GitObject>(notFoundSha));
                repo.TryLookup(notFoundSha).ShouldBeNull();
                repo.TryLookup<GitObject>(notFoundSha).ShouldBeNull();
            }
        }

        [Test]
        public void CanLookupSameObjectTwiceAndTheyAreEqual()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var commit = repo.Lookup(commitSha);
                var commit2 = repo.TryLookup(commitSha);
                commit.Equals(commit2).ShouldBeTrue();
                commit.GetHashCode().ShouldEqual(commit2.GetHashCode());
            }
        }

        [Test]
        public void CanOpenRepoWithFullPath()
        {
            var path = Path.GetFullPath(Constants.TestRepoPath);
            using (new Repository(path))
            {
            }
        }

        [Test]
        public void CanOpenRepository()
        {
            using (new Repository(Constants.TestRepoPath))
            {
            }
        }

        [Test]
        [Ignore("TODO: fix libgit2 error handling for this to work.")]
        public void LookupObjectByWrongTypeThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                repo.Lookup<Tag>(commitSha);
            }
        }

        [Test]
        public void LookupWithEmptyStringThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Lookup(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.Lookup<GitObject>(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.TryLookup(string.Empty));
                Assert.Throws<ArgumentException>(() => repo.TryLookup<GitObject>(string.Empty));
            }
        }

        [Test]
        public void LookupWithNullThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Lookup(null));
                Assert.Throws<ArgumentNullException>(() => repo.TryLookup(null));
                Assert.Throws<ArgumentNullException>(() => repo.Lookup<Commit>(null));
                Assert.Throws<ArgumentNullException>(() => repo.TryLookup<Commit>(null));
            }
        }

        [Test]
        public void OpenNonExistentRepoThrows()
        {
            Assert.Throws<ArgumentException>(() => { new Repository("a_bad_path"); });
        }

        [Test]
        public void OpeningRepositoryWithEmptyPathThrows()
        {
            Assert.Throws<ArgumentException>(() => new Repository(string.Empty));
        }

        [Test]
        public void OpeningRepositoryWithNullPathThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new Repository(null));
        }
    }
}