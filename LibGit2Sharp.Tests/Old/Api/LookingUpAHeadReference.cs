/*
 * The MIT License
 *
 * Copyright (c) 2011 LibGit2Sharp committers
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

using NUnit.Framework;

namespace LibGit2Sharp.Tests.Api
{
    [TestFixture]
    public class LookingUpAHeadReference : ReadOnlyRepositoryFixtureBase
    {
        [TestCase("HEAD", true, "refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        [TestCase("HEAD", false, "HEAD", "refs/heads/master")]
        [TestCase("head-tracker", true, "refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        [TestCase("head-tracker", false, "head-tracker", "HEAD")]
        [TestCase("refs/heads/master", true, "refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        public void ShouldReturnARef(string referenceName, bool shouldPeel, string expectedReferenceName, string expectedTarget)
        {
            using (var repo = new Repository(PathToRepository))
            {
                Ref reference = repo.Refs.Lookup(referenceName, shouldPeel);
                Assert.IsNotNull(reference);
                Assert.AreEqual(expectedReferenceName, reference.CanonicalName);
                Assert.AreEqual(expectedTarget, reference.Target);
            }
        }

        //TODO: This requires some additional testing with a HEAD pointing to a symref pointing to refs/heads/master
        [TestCase("refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        public void ShouldReturnAPeeledRef(string expectedReferenceName, string expectedTarget)
        {
            using (var repo = new Repository(PathToRepository))
            {
                Ref reference = repo.Refs.Head;
                Assert.IsNotNull(reference);
                Assert.AreEqual(expectedReferenceName, reference.CanonicalName);
                Assert.AreEqual(expectedTarget, reference.Target);
            }
        }

        [TestCase("refs//heads//////master", true, "refs/heads/master", "be3563ae3f795b2b4353bcce3a527ad0a4f7f644")]
        public void ShouldNormalizeTheReferenceName(string referenceName, bool shouldPeel, string expectedReferenceName, string expectedTarget)
        {
            using (var repo = new Repository(PathToRepository))
            {
                Ref reference = repo.Refs.Lookup(referenceName, shouldPeel);
                Assert.IsNotNull(reference);
                Assert.AreEqual(expectedReferenceName, reference.CanonicalName);
                Assert.AreEqual(expectedTarget, reference.Target);
            }
        }

        [TestCase("refs/../toto/heads//////master")]
        public void ShouldThrowIfBeingPassedAnInvalidReferenceName(string referenceName)
        {
            using (var repo = new Repository(PathToRepository))
            {
                Assert.Throws<InvalidReferenceNameException>(() => repo.Refs.Lookup(referenceName, false));

            }
        }
    }
}