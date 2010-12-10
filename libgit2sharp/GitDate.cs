using System;

namespace libgit2sharp
{
    public class GitDate
    {
        public GitDate(int secondsSinceEpoch, int timeZoneOffsetInMinutes)
        {
            UnixTimeStamp = secondsSinceEpoch;
            TimeZoneOffset = timeZoneOffsetInMinutes;
        }

        public Int32 UnixTimeStamp { get; private set; }
        public Int32 TimeZoneOffset { get; private set; }
    }
}