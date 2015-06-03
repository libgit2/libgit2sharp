using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// The result of a stash application operation.
    /// </summary>
    public enum StashApplyStatus
    {
        /// <summary>
        /// The stash application was successful.
        /// </summary>
        Applied,

        /// <summary>
        /// The stash application ended up with conflicts.
        /// </summary>
        Conflicts,

        /// <summary>
        /// The stash index given was not found.
        /// </summary>
        NotFound,
    }
}
