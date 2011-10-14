using System;

namespace LibGit2Sharp.Core
{
    internal static class EnumExtensions
    {
        public static bool Has<T>(this Enum enumInstance, T entry)
        {
            return ((int)(object)enumInstance & (int)(object)entry) == (int)(object)(entry);
        }
    }
}
