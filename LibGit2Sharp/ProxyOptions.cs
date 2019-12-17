namespace LibGit2Sharp
{
    /// <summary>
    /// Collection of parameters controlling proxy behavior.
    /// </summary>
    public sealed class ProxyOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ProxyOptions()
        { }

        /// <summary>
        /// The type of proxy to use, by URL, auto-detect.
        /// </summary>
        public ProxyType ProxyType { get; set; }

        /// <summary>
        /// The URL of the proxy. (ProxyType must be Specified)
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// The type of proxy to use.
    /// </summary>
    public enum ProxyType
    {
        /// <summary>
        /// Do not attempt to connect through a proxy
        /// If built against libcurl, it itself may attempt to connect
        /// to a proxy if the environment variables specify it.
        /// </summary>
        None = 0,
        /// <summary>
        /// Try to auto-detect the proxy from the git configuration.
        /// </summary>
        Auto = 1,
        /// <summary>
        /// Connect via the URL given in the options
        /// </summary>
        Specified = 2
    }
}
