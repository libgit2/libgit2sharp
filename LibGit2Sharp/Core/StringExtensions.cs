using System;

namespace LibGit2Sharp.Core
{
    internal static class StringExtensions
    {
        public static int OctalToInt32(this string octal)
        {
            return Convert.ToInt32(octal, 8);
        }
    }
}
