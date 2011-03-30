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
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    public static class AssertExtensions
    {
        #region public

        public static void ShouldBeAboutEqualTo(this DateTimeOffset expected, DateTimeOffset current)
        {
            Assert.AreEqual(expected.Date, current.Date);
            Assert.AreEqual(expected.Offset, current.Offset);
            Assert.AreEqual(expected.Hour, current.Hour);
            Assert.AreEqual(expected.Minute, current.Minute);
            Assert.AreEqual(expected.Second, current.Second);
        }

        public static void ShouldBeFalse(this bool currentObject)
        {
            Assert.IsFalse(currentObject);
        }

        public static void ShouldBeNull(this object currentObject)
        {
            Assert.IsNull(currentObject);
        }

        public static void ShouldBeTrue(this bool currentObject)
        {
            Assert.IsTrue(currentObject);
        }

        public static void ShouldEqual(this object compareFrom, object compareTo)
        {
            Assert.AreEqual(compareTo, compareFrom);
        }

        public static void ShouldNotBeNull(this object currentObject)
        {
            Assert.IsNotNull(currentObject);
        }

        public static void ShouldNotEqual(this object compareFrom, object compareTo)
        {
            Assert.AreNotEqual(compareTo, compareFrom);
        }

        #endregion
    }
}