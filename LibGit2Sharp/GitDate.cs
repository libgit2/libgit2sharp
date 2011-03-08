/*
 * This file is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License, version 2,
 * as published by the Free Software Foundation.
 *
 * In addition to the permissions in the GNU General Public License,
 * the authors give you unlimited permission to link the compiled
 * version of this file into combinations with other programs,
 * and to distribute those combinations without any restriction
 * coming from the use of this file.  (The General Public License
 * restrictions do apply in other respects; for example, they cover
 * modification of the file, and distribution when not linked into
 * a combined executable.)
 *
 * This file is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
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
