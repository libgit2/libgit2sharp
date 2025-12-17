using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// The targets of a Tree based diff comparison.
    /// </summary>
    [Flags]
    public enum DiffTargets
    {
        /// <summary>
        /// The repository index.
        /// </summary>
        Index = 1,

        /// <summary>
        /// The working directory.
        /// </summary>
        WorkingDirectory = 2,
    }
}
