using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Calculated status of a submodule in the working directory considering the current <see cref="Repository.Index"/> and the <see cref="Repository.Head"/>.
    /// </summary>
    [Flags]
    public enum SubmoduleStatus
    {
        /// <summary>
        /// No submodule changes detected.
        /// </summary>
        Unmodified = 0,

        /// <summary>
        /// Superproject head contains submodule.
        /// </summary>
        /// <remarks>Can be returned even if ignore is set to "ALL".</remarks>
        InHead = (1 << 0),
        /// <summary>
        /// Superproject index contains submodule.
        /// </summary>
        /// <remarks>Can be returned even if ignore is set to "ALL".</remarks>
        InIndex = (1 << 1),
        /// <summary>
        /// Superproject gitmodules has submodule.
        /// </summary>
        /// <remarks>Can be returned even if ignore is set to "ALL".</remarks>
        InConfig = (1 << 2),
        /// <summary>
        /// Superproject working directory has submodule.
        /// </summary>
        /// <remarks>Can be returned even if ignore is set to "ALL".</remarks>
        InWorkDir = (1 << 3),

        /// <summary>
        /// Submodule is in index, but not in head.
        /// </summary>
        /// <remarks>Can be returned unless ignore is set to "ALL".</remarks>
        IndexAdded = (1 << 4),
        /// <summary>
        /// Submodule is in head, but not in index.
        /// </summary>
        /// <remarks>Can be returned unless ignore is set to "ALL".</remarks>
        IndexDeleted = (1 << 5),
        /// <summary>
        /// Submodule in index and head don't match.
        /// </summary>
        /// <remarks>Can be returned unless ignore is set to "ALL".</remarks>
        IndexModified = (1 << 6),
        /// <summary>
        /// Submodule in working directory is not initialized.
        /// </summary>
        /// <remarks>Can be returned unless ignore is set to "ALL".</remarks>
        WorkDirUninitialized = (1 << 7),
        /// <summary>
        /// Submodule is in working directory, but not index.
        /// </summary>
        /// <remarks>Can be returned unless ignore is set to "ALL".</remarks>
        WorkDirAdded = (1 << 8),
        /// <summary>
        /// Submodule is in index, but not working directory.
        /// </summary>
        /// <remarks>Can be returned unless ignore is set to "ALL".</remarks>
        WorkDirDeleted = (1 << 9),
        /// <summary>
        /// Submodule in index and working directory head don't match.
        /// </summary>
        /// <remarks>Can be returned unless ignore is set to "ALL".</remarks>
        WorkDirModified = (1 << 10),

        /// <summary>
        /// Submodule working directory index is dirty.
        /// </summary>
        /// <remarks>Can only be returned if ignore is "NONE" or "UNTRACKED".</remarks>
        WorkDirFilesIndexDirty = (1 << 11),
        /// <summary>
        /// Submodule working directory has modified files.
        /// </summary>
        /// <remarks>Can only be returned if ignore is "NONE" or "UNTRACKED".</remarks>
        WorkDirFilesModified = (1 << 12),

        /// <summary>
        /// Working directory contains untracked files.
        /// </summary>
        /// <remarks>Can only be returned if ignore is "NONE".</remarks>
        WorkDirFilesUntracked = (1 << 13),
    }
}
