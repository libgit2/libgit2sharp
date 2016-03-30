using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Allows portable-compatible code that uses GetTypeInfo() to compile and work on net40.
    /// </summary>
    internal static class PortableShims
    {
        /// <summary>
        /// Returns the specified type.
        /// </summary>
        internal static Type GetTypeInfo(this Type type) => type;
    }
}
