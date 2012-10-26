using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Calculated status of a filepath in the working directory considering the current <see cref = "Repository.Index" /> and the <see cref="Repository.Head" />.
    /// </summary>
    [Flags]
    public enum FileStatus
    {
        /// <summary>
        ///   The file doesn't exist.
        /// </summary>
        Nonexistent = -1, /* GIT_STATUS_NOTFOUND */

        /// <summary>
        ///   The file hasn't been modified.
        /// </summary>
        Unaltered = 0, /* GIT_STATUS_CURRENT */

        /// <summary>
        ///   New file has been added to the Index. It's unknown from the Head.
        /// </summary>
        Added = (1 << 0), /* GIT_STATUS_INDEX_NEW */

        /// <summary>
        ///   New version of a file has been added to the Index. A previous version exists in the Head.
        /// </summary>
        Staged = (1 << 1), /* GIT_STATUS_INDEX_MODIFIED */

        /// <summary>
        ///   The deletion of a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        Removed = (1 << 2), /* GIT_STATUS_INDEX_DELETED */

        /// <summary>
        ///   The renaming of a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        Renamed = (1 << 3), /* GIT_STATUS_INDEX_RENAMED */

        /// <summary>
        ///   A change in type for a file has been promoted from the working directory to the Index. A previous version exists in the Head.
        /// </summary>
        StagedTypeChange = (1 << 4), /* GIT_STATUS_INDEX_TYPECHANGE */

        /// <summary>
        ///   New file in the working directory, unknown from the Index and the Head.
        /// </summary>
        Untracked = (1 << 7), /* GIT_STATUS_WT_NEW */

        /// <summary>
        ///   The file has been updated in the working directory. A previous version exists in the Index.
        /// </summary>
        Modified = (1 << 8), /* GIT_STATUS_WT_MODIFIED */

        /// <summary>
        ///   The file has been deleted from the working directory. A previous version exists in the Index.
        /// </summary>
        Missing = (1 << 9), /* GIT_STATUS_WT_DELETED */

        /// <summary>
        ///   The file type has been changed in the working directory. A previous version exists in the Index.
        /// </summary>
        TypeChanged = (1 << 10), /* GIT_STATUS_WT_TYPECHANGE */

        /// <summary>
        ///   The file is <see cref="Untracked"/> but its name and/or path matches an exclude pattern in a <c>gitignore</c> file.
        /// </summary>
        Ignored = (1 << 14), /* GIT_STATUS_IGNORED */
    }
}
