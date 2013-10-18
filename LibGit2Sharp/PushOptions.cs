using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Collection of parameters controlling Push behavior.
    /// </summary>
    public sealed class PushOptions
    {
        /// <summary>
        /// The <see cref="Credentials"/> to authenticate with during the push.
        /// </summary>
        public Credentials Credentials { get; set; }

        /// <summary>
        /// If the transport being used to push to the remote requires the creation
        /// of a pack file, this controls the number of worker threads used by
        /// the packbuilder when creating that pack file to be sent to the remote.
        /// The default is 0, which indicates that the packbuilder will auto-detect
        /// the number of threads to create.
        /// </summary>
        public int PackbuilderDegreeOfParallelism { get; set; }

        /// <summary>
        /// Delegate to report errors when updating references on the remote.
        /// </summary>
        public PushStatusErrorHandler OnPushStatusError { get; set; }

        /// <summary>
        /// Delegate that progress updates of the network transfer portion of push
        /// will be reported through. The frequency of progress updates will not
        /// be more than once every 0.5 seconds (in general).
        /// </summary>
        public PushTransferProgressHandler OnPushTransferProgress { get; set; }

        /// <summary>
        /// Delegate that progress updates of the pack building portion of push
        /// will be reported through. The frequency of progress updates will not
        /// be more than once every 0.5 seconds (in general).
        /// </summary>
        public PackBuilderProgressHandler OnPackBuilderProgress { get; set; }
    }
}
