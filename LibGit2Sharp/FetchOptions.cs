namespace LibGit2Sharp
{
    /// <summary>
    /// Collection of parameters controlling Fetch behavior.
    /// </summary>
    public sealed class FetchOptions : FetchOptionsBase
    {
        /// <summary>
        /// Specifies the tag-following behavior of the fetch operation.
        /// <para>
        /// If not set, the fetch operation will follow the default behavior for the <see cref="Remote"/>
        /// based on the remote's <see cref="Remote.TagFetchMode"/> configuration.
        /// </para>
        /// <para>If neither this property nor the remote `tagopt` configuration is set,
        /// this will default to <see cref="F:TagFetchMode.Auto"/> (i.e. tags that point to objects
        /// retrieved during this fetch will be retrieved as well).</para>
        /// </summary>
        public TagFetchMode? TagFetchMode { get; set; }

        /// <summary>
        /// Specifies the pruning behaviour for the fetch operation
        /// <para>
        /// If not set, the configuration's setting will take effect. If true, the branches which no longer
        /// exist on the remote will be removed from the remote-tracking branches.
        /// </para>
        /// </summary>
        public bool? Prune { get; set; }

        /// <summary>
        /// Get/Set the custom headers.
        /// 
        /// <para> 
        /// This allows you to set custom headers (e.g. X-Forwarded-For, 
        /// X-Request-Id, etc),
        /// </para>
        /// </summary>
        /// <remarks>
        /// Libgit2 sets some headers for HTTP requests (User-Agent, Host, 
        /// Accept, Content-Type, Transfer-Encoding, Content-Length, Accept) that
        /// cannot be overriden.
        /// </remarks>
        /// <example>
        /// var fetchOptions - new FetchOptions() {
        ///     CustomHeaders = new String[] {"X-Request-Id: 12345"}
        /// };
        /// </example>
        /// <value>The custom headers string array</value>
        public string[] CustomHeaders { get; set; }
    }
}
