using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Determines the sorting strategy when iterating through the content of the repository
    /// </summary>
    [Flags]
    [Obsolete("This type will be removed in the next release.")]
    public enum GitSortOptions
    {
        /// <summary>
        /// Sort the repository contents in no particular ordering;
        /// this sorting is arbitrary, implementation-specific
        /// and subject to change at any time.
        /// </summary>
        None = 0,

        /// <summary>
        /// Sort the repository contents in topological order
        /// (parents before children); this sorting mode
        /// can be combined with time sorting.
        /// </summary>
        Topological = (1 << 0),

        /// <summary>
        /// Sort the repository contents by commit time;
        /// this sorting mode can be combined with
        /// topological sorting.
        /// </summary>
        Time = (1 << 1),

        /// <summary>
        /// Iterate through the repository contents in reverse
        /// order; this sorting mode can be combined with
        /// any of the above.
        /// </summary>
        Reverse = (1 << 2)
    }
}
