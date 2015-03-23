using System;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Provides helper methods to help converting between Epoch (unix timestamp) and <see cref="DateTimeOffset"/>.
    /// </summary>
    internal static class Epoch
    {
        private static readonly DateTimeOffset epochDateTimeOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Builds a <see cref="DateTimeOffset"/> from a Unix timestamp and a timezone offset.
        /// </summary>
        /// <param name="secondsSinceEpoch">The number of seconds since 00:00:00 UTC on 1 January 1970.</param>
        /// <param name="timeZoneOffsetInMinutes">The number of minutes from UTC in a timezone.</param>
        /// <returns>A <see cref="DateTimeOffset"/> representing this instant.</returns>
        public static DateTimeOffset ToDateTimeOffset(long secondsSinceEpoch, int timeZoneOffsetInMinutes)
        {
            DateTimeOffset utcDateTime = epochDateTimeOffset.AddSeconds(secondsSinceEpoch);
            TimeSpan offset = TimeSpan.FromMinutes(timeZoneOffsetInMinutes);
            return new DateTimeOffset(utcDateTime.DateTime.Add(offset), offset);
        }

        /// <summary>
        /// Converts the<see cref="DateTimeOffset.UtcDateTime"/> part of a <see cref="DateTimeOffset"/> into a Unix timestamp.
        /// </summary>
        /// <param name="date">The <see cref="DateTimeOffset"/> to convert.</param>
        /// <returns>The number of seconds since 00:00:00 UTC on 1 January 1970.</returns>
        public static Int32 ToSecondsSinceEpoch(this DateTimeOffset date)
        {
            DateTimeOffset utcDate = date.ToUniversalTime();
            return (Int32)utcDate.Subtract(epochDateTimeOffset).TotalSeconds;
        }
    }
}
