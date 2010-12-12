using System;

namespace libgit2sharp
{
    public class GitDate : IEquatable<GitDate>, IEquatable<DateTimeOffset>
    {
        public GitDate(Int32 secondsSinceEpoch, int timeZoneOffsetInMinutes)
        {
            UnixTimeStamp = secondsSinceEpoch;
            TimeZoneOffset = timeZoneOffsetInMinutes;
        }

        public GitDate(Int32 secondsSinceEpoch)
            : this(secondsSinceEpoch, 0)
        {
        }

        public Int32 UnixTimeStamp { get; private set; }
        public Int32 TimeZoneOffset { get; private set; }

        public static explicit operator DateTimeOffset(GitDate date)
        {
            return Epoch.ToDateTimeOffset(date.UnixTimeStamp, date.TimeZoneOffset);
        }

        public static explicit operator GitDate(DateTimeOffset date)
        {
            return Epoch.ToGitDate(date);
        }

        public bool Equals(DateTimeOffset other)
        {
            return Equals((GitDate)other);
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