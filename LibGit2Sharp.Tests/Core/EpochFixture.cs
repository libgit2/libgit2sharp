/*
 * The MIT License
 *
 * Copyright (c) 2011 LibGit2Sharp committers
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
using LibGit2Sharp.Core;
using NUnit.Framework;

namespace LibGit2Sharp.Tests.Core
{
    [TestFixture]
    public class EpochFixture
    {
        [TestCase(0)]
        [TestCase(17)]
        public void UnixTimestampShouldBeCastIntoAUtcBasedDateTimeOffset(long secondsSinceEpoch)
        {
            var date = Epoch.ToDateTimeOffset(secondsSinceEpoch, 0);
            Assert.AreEqual(0, date.Offset.TotalMinutes);

            Assert.AreEqual(TimeSpan.Zero, date.Offset);
            Assert.AreEqual(DateTimeKind.Utc, date.UtcDateTime.Kind);
        }

        [TestCase(0, 0)]
        [TestCase(17, -120)]
        [TestCase(31, 60)]
        public void AreEqual(long secondsSinceEpoch, int timezoneOffset)
        {
            var one = Epoch.ToDateTimeOffset(secondsSinceEpoch, timezoneOffset);
            var another = Epoch.ToDateTimeOffset(secondsSinceEpoch, timezoneOffset);
            
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
        public void UnixTimestampShouldShouldBeCastIntoAPlainUtcDate(long secondsSinceEpoch, string expected)
        {
            var expectedDate = DateTimeOffset.Parse(expected);

            var date = Epoch.ToDateTimeOffset(secondsSinceEpoch, 0);
            
            Assert.AreEqual(secondsSinceEpoch, Epoch.ToSecondsSinceEpoch(date));
            Assert.AreEqual(expectedDate, date);
            Assert.AreEqual(TimeSpan.Zero, date.Offset);
        }

        [TestCase(1250379778, -210, "Sat, 15 Aug 2009 20:12:58 -0330")]
        public void UnixTimestampAndTimezoneOffsetShouldBeCastIntoAUtcDateBearingAnOffset(long secondsSinceEpoch, Int32 offset, string expected)
        {
            var expectedDate = DateTimeOffset.Parse(expected);

            var date = Epoch.ToDateTimeOffset(secondsSinceEpoch, offset);
            Assert.AreEqual(offset, date.Offset.TotalMinutes);
            Assert.AreEqual(secondsSinceEpoch, Epoch.ToSecondsSinceEpoch(date));

            Assert.AreEqual(expectedDate, date);
            Assert.AreEqual(expectedDate.Offset, date.Offset);
        }

        [TestCase("Wed, 08 Dec 2010 09:52:32 GMT", 1291801952, 0)]
        [TestCase("Fri, 13 Feb 2009 23:31:30 GMT", 1234567890, 0)]
        [TestCase("Tue, 26 Oct 2010 17:33:03 GMT", 1288114383, 0)]
        [TestCase("Sat, 14 Feb 2009 00:31:30 +0100", 1234567890, 60)]
        [TestCase("Sat, 15 Aug 2009 20:12:58 -0330", 1250379778, -210)]
        [TestCase("Sat, 15 Aug 2009 23:42:58 GMT", 1250379778, 0)]
        [TestCase("Sun, 16 Aug 2009 00:42:58 +0100", 1250379778, 60)]
        public void DateTimeOffsetShoudlBeCastIntoAUnixTimestampAndATimezoneOffset(string formattedDate, long expectedSeconds, Int32 expectedOffset)
        {
            var when = DateTimeOffset.Parse(formattedDate);
            var date = Epoch.ToDateTimeOffset(expectedSeconds, expectedOffset);
            Assert.AreEqual(when, date);
        }
    }
}
