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
    public class GitDate : IEquatable<GitDate>
    {
        public GitDate(Int32 secondsSinceEpoch, int timeZoneOffsetInMinutes)
        {
            UnixTimeStamp = secondsSinceEpoch;
            TimeZoneOffset = timeZoneOffsetInMinutes;
        }

        public Int32 UnixTimeStamp { get; private set; }
        public Int32 TimeZoneOffset { get; private set; }

        public DateTimeOffset ToDateTimeOffset()
        {
            return Epoch.ToDateTimeOffset(this.UnixTimeStamp, this.TimeZoneOffset);
        }
 
        public bool Equals(GitDate other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.UnixTimeStamp == UnixTimeStamp && other.TimeZoneOffset == TimeZoneOffset;
        }

        public override bool Equals(object obj)
        {
            if (obj is DateTimeOffset)
            {
                return Equals((DateTimeOffset) obj);
            }

            return Equals(obj as GitDate);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UnixTimeStamp*397) ^ TimeZoneOffset;
            }
        }

        public static bool operator ==(GitDate left, GitDate right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GitDate left, GitDate right)
        {
            return !Equals(left, right);
        }
    }
}
