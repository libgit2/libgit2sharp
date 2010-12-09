using System;

namespace libgit2sharp
{
    public static class Epoch
    {
        private static readonly DateTimeOffset EpochDateTimeOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static DateTimeOffset ToDateTimeOffset(Int32 secondsSinceEpoch)
        {
            return EpochDateTimeOffset.AddSeconds(secondsSinceEpoch);
        }

        public static Int32 ToInt32(DateTimeOffset date)
        {
            DateTimeOffset utcDate = date.ToUniversalTime();
            return (Int32)utcDate.Subtract(EpochDateTimeOffset).TotalSeconds;
        }
    }
}