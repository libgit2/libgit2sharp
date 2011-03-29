using System;

namespace LibGit2Sharp
{
    public static class Epoch
    {
        private static readonly DateTimeOffset EpochDateTimeOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static DateTimeOffset ToDateTimeOffset(long secondsSinceEpoch, int timeZoneOffsetInMinutes)
        {
            var utcDateTime = EpochDateTimeOffset.AddSeconds(secondsSinceEpoch);
            var offset = TimeSpan.FromMinutes(timeZoneOffsetInMinutes);
            return new DateTimeOffset(utcDateTime.DateTime.Add(offset), offset);
        }

        public static Int32 ToSecondsSinceEpoch(this DateTimeOffset date)
        {
            var utcDate = date.ToUniversalTime();
            return (Int32) utcDate.Subtract(EpochDateTimeOffset).TotalSeconds;
        }
    }
}