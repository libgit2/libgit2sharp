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
    }
}