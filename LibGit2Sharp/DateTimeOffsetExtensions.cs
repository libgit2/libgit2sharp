using System;

namespace LibGit2Sharp
{
    public static class DateTimeOffsetExtensions
    {
        public static GitDate ToGitDate(this DateTimeOffset dateTimeOffset)
        {
            return Epoch.ToGitDate(dateTimeOffset);
        }
    }
}