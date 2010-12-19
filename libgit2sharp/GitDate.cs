using System;

namespace libgit2sharp
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