using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Additional behaviors the diffing should take into account
    /// when performing the comparison.
    /// </summary>
    [Flags]
    internal enum DiffModifiers
    {
        /// <summary>
        /// No special behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Include untracked files among the files to be processed, when
        /// diffing against the working directory.
        /// </summary>
        IncludeUntracked = (1 << 1),

        /// <summary>
        /// Include unmodified files among the files to be processed, when
        /// diffing against the working directory.
        /// </summary>
        IncludeUnmodified = (1 << 2),

        /// <summary>
        /// Treats the passed pathspecs as explicit paths (no pathspec match).
        /// </summary>
        DisablePathspecMatch = (1 << 3),

        /// <summary>
        /// Include ignored files among the files to be processed, when
        /// diffing against the working directory.
        /// </summary>
        IncludeIgnored = (1 << 4),

        /// <summary>
        /// Specifies that no rename heuristics will be used when checking for
        /// renames, renames being matched only on unmodified renamed files.
        /// </summary>
        FindExactRenames = (1 << 5),

        /// <summary>
        /// Specifies that no renames will be searched for when running blame.
        /// </summary>
        FindNoRenames = (1 << 6),
    }
}
