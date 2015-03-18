using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A status entry from libgit2.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class GitStatusEntry
    {
        /// <summary>
        /// Calculated status of a filepath in the working directory considering the current <see cref = "Repository.Index" /> and the <see cref="Repository.Head" />.
        /// </summary>
        public FileStatus Status;

        /// <summary>
        /// The difference between the <see cref="Repository.Head" /> and <see cref = "Repository.Index" />.
        /// </summary>
        public IntPtr HeadToIndexPtr;

        /// <summary>
        /// The difference between the <see cref = "Repository.Index" /> and the working directory.
        /// </summary>
        public IntPtr IndexToWorkDirPtr;
    }
}
