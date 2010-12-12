using System;

namespace libgit2sharp
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