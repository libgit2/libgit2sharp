using System;

namespace libgit2sharp
{
    public static class EpochHelper
    {
        public static readonly long EpochTicks = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks;

        public static DateTimeOffset ToDateTimeOffset(Int32 secondsSinceEpoch)
        {
            long ticks = secondsSinceEpoch * TimeSpan.TicksPerSecond + EpochTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }
}