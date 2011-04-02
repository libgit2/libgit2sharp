using System;
using NUnit.Framework;

namespace LibGit2Sharp.Tests.TestHelpers
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

        public static void ShouldEqual<T>(this T compareFrom, T compareTo)
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