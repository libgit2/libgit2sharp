using System;
using System.Collections.Generic;
using System.Linq;

namespace LibGit2Sharp.Core
{
    internal static class EnumExtensions
    {
        public static bool HasAny(this Enum enumInstance, IEnumerable<Enum> entries)
        {
            return entries.Any(enumInstance.HasFlag);
        }
    }
}
