/*
 * The MIT License
 *
 * Copyright (c) 2011 Emeric Fermas
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.IO;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class InstantiatingARepository : ReadOnlyRepositoryFixtureBase
    {
        [Test]
        public void ShouldThrowIfPassedANonValidGitDirectory()
        {
            var notAValidRepo = Path.GetTempPath();
            Assert.Throws<NotAValidRepositoryException>(() => new Repository(notAValidRepo));
        }

        [Test]
        public void ShouldThrowIfPassedANonExistingFolder()
        {
            var notAValidRepo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
            Assert.Throws<NotAValidRepositoryException>(() => new Repository(notAValidRepo));
        }

        [Test]
        public void ShouldAcceptPlatormNativeRelativePath()
        {
            string repoPath = PathToRepository.Replace('/', Path.DirectorySeparatorChar);

            AssertRepositoryPath(repoPath);
        }

        [Test]
        public void ShouldAcceptPlatormNativeAbsolutePath()
        {
            string repoPath = Path.GetFullPath(PathToRepository);

            AssertRepositoryPath(repoPath);
        }

        [Test]
        public void ShouldAcceptPlatormNativeRelativePathWithATrailingDirectorySeparatorChar()
        {
            string repoPath = PathToRepository.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            AssertRepositoryPath(repoPath);
        }

        [Test]
        public void ShouldAcceptPlatormNativeAbsolutePathWithATrailingDirectorySeparatorChar()
        {
            string repoPath = Path.GetFullPath(PathToRepository) + Path.DirectorySeparatorChar;

            AssertRepositoryPath(repoPath);
        }

        private static void AssertRepositoryPath(string repoPath)
        {
            var expected = new DirectoryInfo(repoPath);

            DirectoryInfo current;
            
            using (var repo = new Repository(repoPath))
            {
                current = new DirectoryInfo(repo.Details.RepositoryDirectory);
            }

            Assert.AreEqual(expected, current);
        }
    }
}