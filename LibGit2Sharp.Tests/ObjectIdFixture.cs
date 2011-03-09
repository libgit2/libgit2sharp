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
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ObjectIdFixture
    {
        [TestCase("DDelORu/9Dw38NA3GCOlUJ7tWx0=", "0c37a5391bbff43c37f0d0371823a5509eed5b1d")]
        [TestCase("FqASNFZ4mrze9Ld1ITwjqL109eA=", "16a0123456789abcdef4b775213c23a8bd74f5e0")]
        public void ToString(string encoded, string expected)
        {
            byte[] id = Convert.FromBase64String(encoded);

            string objectId = ObjectId.ToString(id);

            Assert.AreEqual(expected, objectId);
        }

        [TestCase("0c37a5391bbff43c37f0d0371823a5509eed5b1d", "DDelORu/9Dw38NA3GCOlUJ7tWx0=")]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e0", "FqASNFZ4mrze9Ld1ITwjqL109eA=")]
        public void ToByteArray(string objectId, string expected)
        {
            byte[] id = Convert.FromBase64String(expected);

            byte[] rawId = ObjectId.ToByteArray(objectId);

            CollectionAssert.AreEqual(id, rawId);
        }

        [TestCase("0c37a5391bbff43c37f0d0371823a5509eed5b1d", true)]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e0", true)]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e", false)]
        [TestCase("16a0123456789abcdef4b775213c23a8bd74f5e01", false)]
        [TestCase("16=0123456789abcdef4b775213c23a8bd74f5e0", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsValid(string objectId, bool expected)
        {
            bool result = ObjectId.IsValid(objectId);

            Assert.AreEqual(expected, result);
        }
    }
}