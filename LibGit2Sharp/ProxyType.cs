namespace LibGit2Sharp
{
    /// <summary>
    /// The type of proxy to use.
    /// </summary>
    public enum ProxyType
    {
        /// <summary>
        /// Do not attempt to connect through a proxy.
        /// </summary>
        None,

        /// <summary>
        /// Try to auto-detect the proxy from the git configuration.
        /// </summary>
        Auto,

        /// <summary>
        /// Connect via the URL given in the options.
        /// </summary>
        Specified
    }
}
