namespace LibGit2Sharp
{
    /// <summary>
    ///   git_remote_completion types.
    /// </summary>
    public enum RemoteCompletionType
    {
        /// <summary>
        ///   Download.
        /// </summary>
        Download = 0, /* GIT_REMOTE_COMPLETION_DOWNLOAD */

        /// <summary>
        ///   Indexing.
        /// </summary>
        Indexing, /* GIT_REMOTE_COMPLETION_INDEXING */

        /// <summary>
        ///   Error.
        /// </summary>
        Error,    /* GIT_REMOTE_COMPLETION_ERROR */
    }
}
