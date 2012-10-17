using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Enum for TagOptions
    /// </summary>
    public enum TagFetchMode
    {
        /// <summary>
        ///   None.
        /// </summary>
        None = 1,  // GIT_REMOTE_DOWNLOAD_TAGS_NONE

        /// <summary>
        ///   Auto.
        /// </summary>
        Auto,  // GIT_REMOTE_DOWNLOAD_TAGS_AUTO

        /// <summary>
        ///   All.
        /// </summary>
        All,   // GIT_REMOTE_DOWNLOAD_TAGS_ALL
    }
}
