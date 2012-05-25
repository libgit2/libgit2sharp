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

        public static void ShouldNotEqual(this object compareFrom, object compareTo)
        {
            Assert.NotEqual(compareTo, compareFrom);
        }
    }
}
