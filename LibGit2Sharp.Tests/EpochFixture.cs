using System;
using LibGit2Sharp.Core;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class EpochFixture
    {
        [Theory]
        [InlineData(0)]
        [InlineData(17)]
        public void UnixTimestampShouldBeCastIntoAUtcBasedDateTimeOffset(long secondsSinceEpoch)
        {
            DateTimeOffset date = Epoch.ToDateTimeOffset(secondsSinceEpoch, 0);
            Assert.Equal(0, date.Offset.TotalMinutes);

            Assert.Equal(TimeSpan.Zero, date.Offset);
            Assert.Equal(DateTimeKind.Utc, date.UtcDateTime.Kind);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(17, -120)]
        [InlineData(31, 60)]
        public void AreEqual(long secondsSinceEpoch, int timezoneOffset)
        {
            DateTimeOffset one = Epoch.ToDateTimeOffset(secondsSinceEpoch, timezoneOffset);
            DateTimeOffset another = Epoch.ToDateTimeOffset(secondsSinceEpoch, timezoneOffset);

            Assert.Equal(one, another);
            Assert.Equal(another, one);

            Assert.True(one == another);
            Assert.True(another == one);

            Assert.False(one != another);
            Assert.False(another != one);

            Assert.Equal(one.GetHashCode(), another.GetHashCode());
        }

        [Theory]
        [InlineData(1291801952, "Wed, 08 Dec 2010 09:52:32 +0000")]
        [InlineData(1234567890, "Fri, 13 Feb 2009 23:31:30 +0000")]
        [InlineData(1288114383, "Tue, 26 Oct 2010 17:33:03 +0000")]
        public void UnixTimestampShouldShouldBeCastIntoAPlainUtcDate(long secondsSinceEpoch, string expected)
        {
            DateTimeOffset expectedDate = DateTimeOffset.Parse(expected);

            DateTimeOffset date = Epoch.ToDateTimeOffset(secondsSinceEpoch, 0);

            Assert.Equal(secondsSinceEpoch, date.ToSecondsSinceEpoch());
            Assert.Equal(expectedDate, date);
            Assert.Equal(TimeSpan.Zero, date.Offset);
        }

        [Theory]
        [InlineData(1250379778, -210, "Sat, 15 Aug 2009 20:12:58 -0330")]
        public void UnixTimestampAndTimezoneOffsetShouldBeCastIntoAUtcDateBearingAnOffset(long secondsSinceEpoch, Int32 offset, string expected)
        {
            DateTimeOffset expectedDate = DateTimeOffset.Parse(expected);

            DateTimeOffset date = Epoch.ToDateTimeOffset(secondsSinceEpoch, offset);
            Assert.Equal(offset, date.Offset.TotalMinutes);
            Assert.Equal(secondsSinceEpoch, date.ToSecondsSinceEpoch());

            Assert.Equal(expectedDate, date);
            Assert.Equal(expectedDate.Offset, date.Offset);
        }

        [Theory]
        [InlineData("Wed, 08 Dec 2010 09:52:32 +0000", 1291801952, 0)]
        [InlineData("Fri, 13 Feb 2009 23:31:30 +0000", 1234567890, 0)]
        [InlineData("Tue, 26 Oct 2010 17:33:03 +0000", 1288114383, 0)]
        [InlineData("Sat, 14 Feb 2009 00:31:30 +0100", 1234567890, 60)]
        [InlineData("Sat, 15 Aug 2009 20:12:58 -0330", 1250379778, -210)]
        [InlineData("Sat, 15 Aug 2009 23:42:58 +0000", 1250379778, 0)]
        [InlineData("Sun, 16 Aug 2009 00:42:58 +0100", 1250379778, 60)]
        public void DateTimeOffsetShoudlBeCastIntoAUnixTimestampAndATimezoneOffset(string formattedDate, long expectedSeconds, Int32 expectedOffset)
        {
            DateTimeOffset when = DateTimeOffset.Parse(formattedDate);
            DateTimeOffset date = Epoch.ToDateTimeOffset(expectedSeconds, expectedOffset);
            Assert.Equal(when, date);
        }
    }
}
