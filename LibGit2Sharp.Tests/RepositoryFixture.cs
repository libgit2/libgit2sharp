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
using System.IO;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RepositoryFixture
    {
        private const string newRepoPath = "new_repo";

        [TestCase]
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

        [Test]
        public void CallingExistsWithNullThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Exists(null));
            }
        }

        [Test]
        public void CallingExistsWithEmptyThrows()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Exists(string.Empty));
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