using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Collection of parameters controlling Fetch behavior.
    /// </summary>
    public sealed class FetchOptions
    {
        /// <summary>
        /// Specifies the tag-following behavior of the fetch operation.
        /// <para>
        /// If not set, the fetch operation will follow the default behavior for the <see cref="Remote"/>
        /// based on the remote's <see cref="Remote.TagFetchMode"/> configuration.
        /// </para>
        /// <para>If neither this property nor the remote `tagopt` configuration is set,
        /// this will default to <see cref="TagFetchMode.Auto"/> (i.e. tags that point to objects
        /// retrieved during this fetch will be retrieved as well).</para>
        /// </summary>
        public TagFetchMode? TagFetchMode { get; set; }

        /// <summary>
        /// Delegate that progress updates of the network transfer portion of fetch
        /// will be reported through.
        /// </summary>
        public ProgressHandler OnProgress { get; set; }

        /// <summary>
        /// Delegate that updates of remote tracking branches will be reported through.
        /// </summary>
        public UpdateTipsHandler OnUpdateTips { get; set; }

        /// <summary>
        /// Callback method that transfer progress will be reported through.
        /// <para>
        /// Reports the client's state regarding the received and processed (bytes, objects) from the server.
        /// </para>
        /// </summary>
        public TransferProgressHandler OnTransferProgress { get; set; }

        /// <summary>
        /// Credentials to use for username/password authentication.
        /// </summary>
        public Credentials Credentials { get; set; }
    }
}
