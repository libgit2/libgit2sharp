using System;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class EpochFixture
    {
        [TestCase(0)]
        [TestCase(17)]
        public void ToDateDateTimeOffset_ShouldReturnAUtcBasedDateTimeOffset(Int32 secondsSinceEpoch)
        {
            DateTimeOffset when = Epoch.ToDateTimeOffset(secondsSinceEpoch);
            Assert.AreEqual(TimeSpan.Zero, when.Offset);
            Assert.AreEqual(DateTimeKind.Utc, when.UtcDateTime.Kind);
        }

        [TestCase(1291801952, "Wed, 08 Dec 2010 09:52:32 GMT")]
        [TestCase(1234567890, "Fri, 13 Feb 2009 23:31:30 GMT")]
        [TestCase(1288114383, "Tue, 26 Oct 2010 17:33:03 GMT")]
        public void ToDateDateTimeOffset_ShouldCorrectlyConvertAPlainUtcDate(Int32 secondsSinceEpoch, string expected)
        {
            DateTimeOffset when = Epoch.ToDateTimeOffset(secondsSinceEpoch);
            var expectedDate = DateTimeOffset.Parse(expected);
            Assert.AreEqual(expectedDate, when);
            Assert.AreEqual(TimeSpan.Zero, when.Offset);
        }

        [TestCase(1250379778, -210, "Sat, 15 Aug 2009 20:12:58 -0330")]
        public void ToDateDateTimeOffset_ShouldCorrectlyConvertAUtcDateWithATimeZoneOffset(Int32 secondsSinceEpoch, Int32 offset, string expected)
        {
            DateTimeOffset when = Epoch.ToDateTimeOffset(secondsSinceEpoch, offset);
            var expectedDate = DateTimeOffset.Parse(expected);
            Assert.AreEqual(expectedDate, when);
            Assert.AreEqual(expectedDate.Offset, when.Offset);
        }

        [TestCase("Wed, 08 Dec 2010 09:52:32 GMT", 1291801952)]
        [TestCase("Fri, 13 Feb 2009 23:31:30 GMT", 1234567890)]
        [TestCase("Tue, 26 Oct 2010 17:33:03 GMT", 1288114383)]
        [TestCase("Sat, 14 Feb 2009 00:31:30 +0100", 1234567890)]
        [TestCase("Sat, 15 Aug 2009 20:12:58 -0330", 1250379778)]
        [TestCase("Sat, 15 Aug 2009 23:42:58 GMT", 1250379778)]
        [TestCase("Sun, 16 Aug 2009 00:42:58 +0100", 1250379778)]
        public void ToInt32_ShouldCorrectlyConvert(string formattedDate, Int32 expected)
        {
            var date = DateTimeOffset.Parse(formattedDate);

            Int32 when = Epoch.ToInt32(date);
            Assert.AreEqual(expected, when);
        }
    }
}
