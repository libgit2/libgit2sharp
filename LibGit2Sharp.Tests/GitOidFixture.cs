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

using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class GitOidFixture
    {
        [Test]
        public void CanConvertOidToSha()
        {
            var oid = new GitOid {Id = new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9}};
            oid.ToSha().ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            oid.ToString().ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
        }

        [Test]
        public void CanConvertShaToOid()
        {
            GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09").Id.ShouldEqual(new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9});
        }

        [Test]
        public void SameObjectsAreEqual()
        {
            var a = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            var b = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            (a.Equals(b)).ShouldBeTrue();
        }

        [Test]
        public void SameObjectsHaveSameHashCode()
        {
            var a = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            var b = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            a.GetHashCode().ShouldEqual(b.GetHashCode());
        }
    }
}