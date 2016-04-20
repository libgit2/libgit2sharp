using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A status entry from libgit2.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_status_entry
    {
        /// <summary>
        /// Calculated status of a filepath in the working directory considering the current <see cref = "Repository.Index" /> and the <see cref="Repository.Head" />.
        /// </summary>
        public FileStatus status;

        /// <summary>
        /// The difference between the <see cref="Repository.Head" /> and <see cref = "Repository.Index" />.
        /// </summary>
        public git_diff_delta* head_to_index;

        /// <summary>
        /// The difference between the <see cref = "Repository.Index" /> and the working directory.
        /// </summary>
        public git_diff_delta* index_to_workdir;
    }
}
