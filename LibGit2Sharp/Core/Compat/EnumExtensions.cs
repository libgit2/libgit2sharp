using System;

namespace LibGit2Sharp.Core.Compat
{
    internal static class EnumExtensions
    {
        // Based on the following Stack Overflow post
        // http://stackoverflow.com/questions/93744/most-common-c-sharp-bitwise-operations-on-enums/417217#417217
        // Reused with permission of Hugo Bonacci on Jan 04 2013.
        public static bool HasFlag<T>(this Enum enumInstance, T entry)
        {
            return ((int)(object)enumInstance & (int)(object)entry) == (int)(object)(entry);
        }
    }
}
