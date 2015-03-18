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
    }
}
