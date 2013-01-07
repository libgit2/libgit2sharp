using System;
using System.Collections.Generic;
using System.Linq;

namespace LibGit2Sharp.Core
{
    internal static class EnumExtensions
    {
        // Based on the following Stack Overflow post
        // http://stackoverflow.com/questions/93744/most-common-c-sharp-bitwise-operations-on-enums/417217#417217
        // Reused with permission of Hugo Bonacci on Jan 04 2013.
        public static bool Has<T>(this Enum enumInstance, T entry)
        {
            return ((int)(object)enumInstance & (int)(object)entry) == (int)(object)(entry);
        }

        public static bool HasAny<T>(this Enum enumInstance, IEnumerable<T> entries)
        {
            return entries.Any(enumInstance.Has);
        }
    }
}
