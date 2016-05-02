using System;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset TruncateMilliseconds(this DateTimeOffset dto)
        {
            // From http://stackoverflow.com/a/1005222/335418

            return dto.AddTicks( - (dto.Ticks % TimeSpan.TicksPerSecond));
        }
    }
}
