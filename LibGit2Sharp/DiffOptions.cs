using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Additional behaviors the diffing should take into account
    ///   when performing the comparison.
    /// </summary>
    [Flags]
    internal enum DiffOptions
    {
        /// <summary>
        ///   No special behavior.
        /// </summary>
        None,

        /// <summary>
        ///   Include untracked files among the files to be processed, when
        ///   diffing against the working directory.
        /// </summary>
        IncludeUntracked,
    }
}
