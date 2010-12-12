using System;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class GitDateFixture
    {
        [TestCase(0)]
        [TestCase(17)]
        public void UnixTimestampShouldBeCastIntoAUtcBasedDateTimeOffset(Int32 secondsSinceEpoch)
        {
            var date = new GitDate(secondsSinceEpoch);
            Assert.AreEqual(0, date.TimeZoneOffset);

            var when = (DateTimeOffset)date;

            Assert.AreEqual(TimeSpan.Zero, when.Offset);
            Assert.AreEqual(DateTimeKind.Utc, when.UtcDateTime.Kind);
        }

        [TestCase(0, 0)]
        [TestCase(17, -120)]
        [TestCase(31, 60)]
        public void AreEqual(Int32 secondsSinceEpoch, int timezoneOffset)
        {
            var one = new GitDate(secondsSinceEpoch, timezoneOffset);
            var another = new GitDate(secondsSinceEpoch, timezoneOffset);
            
            Assert.AreEqual(one, another);
            Assert.AreEqual(another, one);
            
            Assert.IsTrue(one == another);
            Assert.IsTrue(another == one);
            
            Assert.IsFalse(one != another);
            Assert.IsFalse(another != one);

            Assert.AreEqual(one.GetHashCode(), another.GetHashCode());
        }

        [TestCase(1291801952, "Wed, 08 Dec 2010 09:52:32 GMT")]
        [TestCase(1234567890, "Fri, 13 Feb 2009 23:31:30 GMT")]
        [TestCase(1288114383, "Tue, 26 Oct 2010 17:33:03 GMT")]
        public void UnixTimestampShouldShouldBeCastIntoAPlainUtcDate(Int32 secondsSinceEpoch, string expected)
        {
            var expectedDate = DateTimeOffset.Parse(expected);

            var date = new GitDate(secondsSinceEpoch);
            Assert.AreEqual(0, date.TimeZoneOffset);
            Assert.AreEqual(secondsSinceEpoch, date.UnixTimeStamp);

            var when = (DateTimeOffset)date;

            Assert.AreEqual(expectedDate, when);
            Assert.AreEqual(TimeSpan.Zero, when.Offset);
        }

        [TestCase(1250379778, -210, "Sat, 15 Aug 2009 20:12:58 -0330")]
        public void UnixTimestampAndTimezoneOffsetShouldBeCastIntoAUtcDateBearingAnOffset(Int32 secondsSinceEpoch, Int32 offset, string expected)
        {
            var expectedDate = DateTimeOffset.Parse(expected);

            var date = new GitDate(secondsSinceEpoch, offset);
            Assert.AreEqual(offset, date.TimeZoneOffset);
            Assert.AreEqual(secondsSinceEpoch, date.UnixTimeStamp);

            var when = (DateTimeOffset)date;

            Assert.AreEqual(expectedDate, when);
            Assert.AreEqual(expectedDate.Offset, when.Offset);
        }

        [TestCase("Wed, 08 Dec 2010 09:52:32 GMT", 1291801952, 0)]
        [TestCase("Fri, 13 Feb 2009 23:31:30 GMT", 1234567890, 0)]
        [TestCase("Tue, 26 Oct 2010 17:33:03 GMT", 1288114383, 0)]
        [TestCase("Sat, 14 Feb 2009 00:31:30 +0100", 1234567890, 60)]
        [TestCase("Sat, 15 Aug 2009 20:12:58 -0330", 1250379778, -210)]
        [TestCase("Sat, 15 Aug 2009 23:42:58 GMT", 1250379778, 0)]
        [TestCase("Sun, 16 Aug 2009 00:42:58 +0100", 1250379778, 60)]
        public void DateTimeOffsetShoudlBeCastIntoAUnixTimestampAndATimezoneOffset(string formattedDate, Int32 expectedSeconds, Int32 expectedOffset)
        {
            var when = DateTimeOffset.Parse(formattedDate);

            var date = (GitDate)when;

            Assert.AreEqual(expectedSeconds, date.UnixTimeStamp);
            Assert.AreEqual(expectedOffset, date.TimeZoneOffset);
        }
    }
}
