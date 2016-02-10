﻿namespace LibGit2Sharp
{
    /// <summary>
    /// Describe the expected tag retrieval behavior
    /// when a fetch operation is being performed.
    /// </summary>
    public enum TagFetchMode
    {
        /// <summary>
        /// Use the setting from the configuration
        /// or, when there isn't any, fallback to default behavior.
        /// </summary>
        FromConfigurationOrDefault = 0,  // GIT_REMOTE_DOWNLOAD_TAGS_FALLBACK

        /// <summary>
        /// Will automatically retrieve tags that
        /// point to objects retrieved during this fetch.
        /// </summary>
        Auto,  // GIT_REMOTE_DOWNLOAD_TAGS_AUTO

        /// <summary>
        /// No tag will be retrieved.
        /// </summary>
        None,  // GIT_REMOTE_DOWNLOAD_TAGS_NONE

        /// <summary>
        /// All tags will be downloaded, but _only_ tags, along with
        /// all the objects these tags are pointing to.
        /// </summary>
        All,   // GIT_REMOTE_DOWNLOAD_TAGS_ALL
    }
}
