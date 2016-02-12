using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Calculated status of a filepath in the working directory considering the current <see cref="Repository.Index"/> and the <see cref="Repository.Head"/>.
    /// </summary>
    [Flags]
    public enum FileStatus
    {
        /// <summary>
        /// The file doesn't exist.
        /// </summary>
        Nonexistent = (1 << 31),

        /// <summary>
        /// The file hasn't been modified.
        /// </summary>
        Unaltered = 0, /* GIT_STATUS_CURRENT */

        /// <summary>
        /// New file has been added to the Index. It's unknown from the Head.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use NewInIndex instead.")]
        Added = (1 << 0), /* GIT_STATUS_INDEX_NEW */

        /// <summary>
        /// New file has been added to the Index. It's unknown from the Head.
        /// </summary>
        NewInIndex = (1 << 0), /* GIT_STATUS_INDEX_NEW */

        /// <summary>
        /// New version of a file has been added to the Index. A previous version exists in the Head.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use ModifiedInIndex instead.")]
        Staged = (1 << 1), /* GIT_STATUS_INDEX_MODIFIED */

        /// <summary>
        /// New version of a file has been added to the Index. A previous version exists in the Head.
        /// </summary>
        ModifiedInIndex = (1 << 1), /* GIT_STATUS_INDEX_MODIFIED */

        /// <summary>
        /// The deletion of a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use DeletedFromIndex instead.")]
        Removed = (1 << 2), /* GIT_STATUS_INDEX_DELETED */

        /// <summary>
        /// The deletion of a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        DeletedFromIndex = (1 << 2), /* GIT_STATUS_INDEX_DELETED */

        /// <summary>
        /// The renaming of a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        RenamedInIndex = (1 << 3), /* GIT_STATUS_INDEX_RENAMED */

        /// <summary>
        /// A change in type for a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use TypeChangeInIndex instead.")]
        StagedTypeChange = (1 << 4), /* GIT_STATUS_INDEX_TYPECHANGE */

        /// <summary>
        /// A change in type for a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        TypeChangeInIndex = (1 << 4), /* GIT_STATUS_INDEX_TYPECHANGE */

        /// <summary>
        /// New file in the working directory, unknown from the Index and the Head.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use NewInWorkdir instead.")]
        Untracked = (1 << 7), /* GIT_STATUS_WT_NEW */

        /// <summary>
        /// New file in the working directory, unknown from the Index and the Head.
        /// </summary>
        NewInWorkdir = (1 << 7), /* GIT_STATUS_WT_NEW */

        /// <summary>
        /// The file has been updated in the working directory. A previous version exists in the Index.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use ModifiedInWorkdir instead.")]
        Modified = (1 << 8), /* GIT_STATUS_WT_MODIFIED */

        /// <summary>
        /// The file has been updated in the working directory. A previous version exists in the Index.
        /// </summary>
        ModifiedInWorkdir = (1 << 8), /* GIT_STATUS_WT_MODIFIED */

        /// <summary>
        /// The file has been deleted from the working directory. A previous version exists in the Index.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use DeletedFromWorkdir instead.")]
        Missing = (1 << 9), /* GIT_STATUS_WT_DELETED */

        /// <summary>
        /// The file has been deleted from the working directory. A previous version exists in the Index.
        /// </summary>
        DeletedFromWorkdir = (1 << 9), /* GIT_STATUS_WT_DELETED */

        /// <summary>
        /// The file type has been changed in the working directory. A previous version exists in the Index.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use TypeChangeInWorkdir instead.")]
        TypeChanged = (1 << 10), /* GIT_STATUS_WT_TYPECHANGE */

        /// <summary>
        /// The file type has been changed in the working directory. A previous version exists in the Index.
        /// </summary>
        TypeChangeInWorkdir = (1 << 10), /* GIT_STATUS_WT_TYPECHANGE */

        /// <summary>
        /// The file has been renamed in the working directory.  The previous version at the previous name exists in the Index.
        /// </summary>
        RenamedInWorkdir = (1 << 11), /* GIT_STATUS_WT_RENAMED */

        /// <summary>
        /// The file is unreadable in the working directory.
        /// </summary>
        Unreadable = (1 << 12), /* GIT_STATUS_WT_UNREADABLE */

        /// <summary>
        /// The file is <see cref="NewInWorkdir"/> but its name and/or path matches an exclude pattern in a <c>gitignore</c> file.
        /// </summary>
        Ignored = (1 << 14), /* GIT_STATUS_IGNORED */

        /// <summary>
        /// The file is <see cref="Conflicted"/> due to a merge.
        /// </summary>
        Conflicted = (1 << 15), /* GIT_STATUS_CONFLICTED */
    }
}
