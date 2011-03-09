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
    public class GitDateFixture
    {
        [TestCase(0)]
        [TestCase(17)]
        public void UnixTimestampShouldBeCastIntoAUtcBasedDateTimeOffset(Int32 secondsSinceEpoch)
        {
            var date = new GitDate(secondsSinceEpoch, 0);
            Assert.AreEqual(0, date.TimeZoneOffset);

            var when = date.ToDateTimeOffset();

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

            var date = new GitDate(secondsSinceEpoch, 0);
            Assert.AreEqual(0, date.TimeZoneOffset);
            Assert.AreEqual(secondsSinceEpoch, date.UnixTimeStamp);

            var when = date.ToDateTimeOffset();

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

            var when = date.ToDateTimeOffset();

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

            var date = when.ToGitDate();

            Assert.AreEqual(expectedSeconds, date.UnixTimeStamp);
            Assert.AreEqual(expectedOffset, date.TimeZoneOffset);
        }
    }
}
