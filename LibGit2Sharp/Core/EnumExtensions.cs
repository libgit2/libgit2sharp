using System;
using System.Collections.Generic;
using System.Linq;

namespace LibGit2Sharp.Core
{
    internal static class EnumExtensions
    {
        public static bool Has<T>(this Enum enumInstance, T entry)
        {
            return ((int)(object)enumInstance & (int)(object)entry) == (int)(object)(entry);
        }

        public static bool HasAny<T>(this Enum enumInstance, IEnumerable<T> entries)
        {
            return entries.Any(enumInstance.Has<T>);
        }
    }
}
