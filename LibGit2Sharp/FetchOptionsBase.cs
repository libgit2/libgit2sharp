using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Base collection of parameters controlling Fetch behavior.
    /// </summary>
    public abstract class FetchOptionsBase
    {
        internal FetchOptionsBase()
        { }

        /// <summary>
        /// Handler for network transfer and indexing progress information.
        /// </summary>
        public ProgressHandler OnProgress { get; set; }

        /// <summary>
        /// Handler for updates to remote tracking branches.
        /// </summary>
        public UpdateTipsHandler OnUpdateTips { get; set; }

        /// <summary>
        /// Handler for data transfer progress.
        /// <para>
        /// Reports the client's state regarding the received and processed (bytes, objects) from the server.
        /// </para>
        /// </summary>
        public TransferProgressHandler OnTransferProgress { get; set; }

        /// <summary>
        /// Handler to generate <see cref="LibGit2Sharp.Credentials"/> for authentication.
        /// </summary>
        public CredentialsHandler CredentialsProvider { get; set; }

        /// <summary>
        /// This handler will be called to let the user make a decision on whether to allow
        /// the connection to proceed based on the certificate presented by the server.
        /// </summary>
        public CertificateCheckHandler CertificateCheck { get; set; }

        /// <summary>
        /// Starting to operate on a new repository.
        /// </summary>
        public RepositoryOperationStarting RepositoryOperationStarting { get; set; }

        /// <summary>
        /// Completed operating on the current repository.
        /// </summary>
        public RepositoryOperationCompleted RepositoryOperationCompleted { get; set; }

        /// <summary>
        /// Options for connecting through a proxy.
        /// </summary>
        public ProxyOptions ProxyOptions { get; } = new();
    }
}
