using System;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class EpochHelperFixture
    {
        [TestCase(0)]
        [TestCase(17)]
        public void ToDateDateTimeOffset_ShouldReturnAUtcBasedDateTimeOffset(Int32 secondsSinceEpoch)
        {
            DateTimeOffset when = EpochHelper.ToDateTimeOffset(secondsSinceEpoch);
            Assert.AreEqual(TimeSpan.Zero, when.Offset);
            Assert.AreEqual(DateTimeKind.Utc, when.UtcDateTime.Kind);
        }

        [TestCase(1291801952, "Wed, 08 Dec 2010 09:52:32 GMT")]
        [TestCase(1234567890, "Fri, 13 Feb 2009 23:31:30 GMT")]
        [TestCase(1288114383, "Tue, 26 Oct 2010 17:33:03 GMT")]
        public void ToDateDateTimeOffset_ShouldCorrectlyConvert(Int32 secondsSinceEpoch, string expected)
        {
            DateTimeOffset when = EpochHelper.ToDateTimeOffset(secondsSinceEpoch);
            var expectedDate = DateTimeOffset.Parse(expected);
            Assert.AreEqual(expectedDate, when);
        }
    }
}
