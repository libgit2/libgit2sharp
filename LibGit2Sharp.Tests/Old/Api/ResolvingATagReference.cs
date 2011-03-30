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
    public class ResolvingATagReference : ReadOnlyRepositoryFixtureBase
    {
        [TestCase("refs/tags/test", "b25fa35b38051e4ae45d4222e795f9df2e43f1d1")]
        [TestCase("refs/tags/very-simple", "7b4384978d2493e851f9cca7858815fac9b10980")]
        public void ShouldReturnATag(string reference, string expectedId)
        {
            using (var repo = new Repository(PathToRepository))
            {
                var gitObject = repo.Resolve(reference);
                Assert.IsNotNull(gitObject);
                Assert.IsAssignableFrom(typeof(Tag), gitObject);
                Assert.AreEqual(ObjectType.Tag, gitObject.Type);
                Assert.AreEqual(expectedId, gitObject.Id);
            }
        }
    }
}