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

using System.IO;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class InitializingARepository : RepositoryToBeCreatedFixtureBase
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ShouldReturnAValidGitPath(bool isBare)
        {
            var expectedGitDirName = new DirectoryInfo(PathToTempDirectory).Name;

            expectedGitDirName += isBare ? "/" : "/.git/";

            var gitDirPath = Repository.Init(PathToTempDirectory, isBare);
            StringAssert.EndsWith(expectedGitDirName, gitDirPath);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ShouldGenerateAValidRepository(bool isBare)
        {
            var gitDirPath = Repository.Init(PathToTempDirectory, isBare);

            using (var repo = new Repository(gitDirPath))
            {
                Assert.AreEqual(gitDirPath, repo.Details.RepositoryDirectory);
            }
        }
    }
}