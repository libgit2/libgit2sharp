﻿using System;

namespace LibGit2Sharp
{
    [Flags]
    public enum FileStatus
    {
        Nonexistent = -1, /* GIT_STATUS_NOTFOUND */
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
        ///   New file in the working directory, unknown from the Index and the Head.
        /// </summary>
        Untracked = (1 << 3), /* GIT_STATUS_WT_NEW */

        /// <summary>
        ///   The file has been updated in the working directory. A previous version exists in the Index.
        /// </summary>
        Modified = (1 << 4), /* GIT_STATUS_WT_MODIFIED */

        /// <summary>
        ///   The file has been deleted from the working directory. A previous version exists in the Index.
        /// </summary>
        Missing = (1 << 5), /* GIT_STATUS_WT_DELETED */

        //TODO: Ignored files not handled yet
        GIT_STATUS_IGNORED = (1 << 6), /* GIT_STATUS_IGNORED */
    }
}
