using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp.Core
{
    internal static class EnumExtensions
    {
        public static bool HasAny<T>(this Enum enumInstance, IEnumerable<T> entries)
        {
            return entries.Any(enumInstance.HasFlag);
        }
    }
}
