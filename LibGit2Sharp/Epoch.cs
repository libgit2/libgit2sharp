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

namespace LibGit2Sharp
{
    internal static class Epoch
    {
        private static readonly DateTimeOffset EpochDateTimeOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static DateTimeOffset ToDateTimeOffset(Int32 secondsSinceEpoch)
        {
            return EpochDateTimeOffset.AddSeconds(secondsSinceEpoch);
        }

        public static DateTimeOffset ToDateTimeOffset(int secondsSinceEpoch, int timeZoneOffsetInMinutes)
        {
            DateTimeOffset utcDateTime = ToDateTimeOffset(secondsSinceEpoch);
            TimeSpan offset = TimeSpan.FromMinutes(timeZoneOffsetInMinutes);
            return new DateTimeOffset(utcDateTime.DateTime.Add(offset), offset);
        }

        private static Int32 ToInt32(DateTimeOffset date)
        {
            DateTimeOffset utcDate = date.ToUniversalTime();
            return (Int32)utcDate.Subtract(EpochDateTimeOffset).TotalSeconds;
        }

        public static GitDate ToGitDate(DateTimeOffset date)
        {
            Int32 secondsSinceEpoch = ToInt32(date);
            return new GitDate(secondsSinceEpoch, (int)date.Offset.TotalMinutes);
        }
    }
}
