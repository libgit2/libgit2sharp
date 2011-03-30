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

using System.Collections.Generic;
using NUnit.Framework;

namespace LibGit2Sharp.Tests.Api
{
    [TestFixture]
    public class RetrievingAllReferences : ReadOnlyRepositoryFixtureBase
    {
        [Test]
        public void ShouldNotReturnDuplicateRefsWhenTheyExistInBothPackedAndLooseState()
        {
            using (var repo = new Repository(PathToRepository))
            {
                IList<Ref> refs = repo.Refs.RetrieveAll();
                Assert.AreEqual(7, refs.Count); //TODO: This test will pass once https://github.com/libgit2/libgit2/commit/7ad96e51ca81974c417914edbc81a63e390c4301 gets ported
            }
        }
    }
}