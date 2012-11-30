using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The target of a Tree based diff comparison.
    /// </summary>
    [Obsolete("This type will be removed in the next release. Please use DiffTargets instead.")]
    public enum DiffTarget
    {
        /// <summary>
        ///   The working directory.
        /// </summary>
        WorkingDirectory,

        /// <summary>
        ///   The repository index.
        /// </summary>
        Index,

        /// <summary>
        ///   Both the working directory and the repository index.
        /// </summary>
        BothWorkingDirectoryAndIndex,
    }
}
