using System;
using Xunit;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class AssertExtensions
    {
        public static void ShouldBeAboutEqualTo(this DateTimeOffset expected, DateTimeOffset current)
        {
            Assert.Equal(expected.Date, current.Date);
            Assert.Equal(expected.Offset, current.Offset);
            Assert.Equal(expected.Hour, current.Hour);
            Assert.Equal(expected.Minute, current.Minute);
            Assert.Equal(expected.Second, current.Second);
        }

        public static void ShouldBeNull(this object currentObject)
        {
            Assert.Null(currentObject);
        }

        public static void ShouldBeTrue(this bool currentObject)
        {
            Assert.True(currentObject);
        }

        public static void ShouldEqual(this object compareFrom, object compareTo)
        {
            Assert.Equal(compareTo, compareFrom);
        }

        public static void ShouldEqual<T>(this T compareFrom, T compareTo)
        {
            Assert.Equal(compareTo, compareFrom);
        }

        public static void ShouldNotBeNull(this object currentObject)
        {
            Assert.NotNull(currentObject);
        }

        public static void ShouldNotEqual(this object compareFrom, object compareTo)
        {
            Assert.NotEqual(compareTo, compareFrom);
        }
    }
}
