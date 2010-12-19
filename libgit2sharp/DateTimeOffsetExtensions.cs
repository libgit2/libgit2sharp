using System;

namespace libgit2sharp
{
    public static class DateTimeOffsetExtensions
    {
        public static GitDate ToGitDate(this DateTimeOffset dateTimeOffset)
        {
            return Epoch.ToGitDate(dateTimeOffset);
        }
    }
}