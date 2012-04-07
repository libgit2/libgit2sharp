using System;

namespace LibGit2Sharp.Core.Compat
{
    /// <summary>
    /// Provides information about, and means to manipulate, the current environment and platform.
    /// </summary>
    public static class Environment
    {
        /// <summary>
        /// Determines whether the current process is a 64-bit process.
        /// </summary>
        public static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }
    }
}
