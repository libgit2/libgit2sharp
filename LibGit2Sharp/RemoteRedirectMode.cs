namespace LibGit2Sharp
{
    /// <summary>
    /// Remote redirection settings; wehther redirects to another
    /// host are permitted. By default, git will follow a redirect
    /// on the initial request (`/info/refs`) but not subsequent
    /// requests.
    /// </summary>
    public enum RemoteRedirectMode
    {
        /// <summary>
        /// Do not follow any off-site redirects at any stage of
        /// the fetch or push.
        /// </summary>
        None = 0, // GIT_REMOTE_REDIRECT_NONE

        /// <summary>
        /// Allow off-site redirects only upon the initial
        /// request. This is the default.
        /// </summary>
        Auto,     // GIT_REMOTE_REDIRECT_INITIAL

        /// <summary>
        /// Allow redirects at any stage in the fetch or push.
        /// </summary>
        All       // GIT_REMOTE_REDIRECT_ALL
    }
}
