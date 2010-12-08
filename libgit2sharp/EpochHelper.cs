using System;

namespace libgit2sharp
{
    public static class EpochHelper
    {
        public static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static DateTimeOffset ToDateTimeOffset(Int32 secondsSinceEpoch)
        {
            return Epoch.AddSeconds(secondsSinceEpoch);
        }
    }
}