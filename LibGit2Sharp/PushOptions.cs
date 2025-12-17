using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Collection of parameters controlling Push behavior.
    /// </summary>
    public sealed class PushOptions
    {
        /// <summary>
        /// Handler to generate <see cref="LibGit2Sharp.Credentials"/> for authentication.
        /// </summary>
        public CredentialsHandler CredentialsProvider { get; set; }

        /// <summary>
        /// This handler will be called to let the user make a decision on whether to allow
        /// the connection to preoceed based on the certificate presented by the server.
        /// </summary>
        public CertificateCheckHandler CertificateCheck { get; set; }

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

        /// <summary>
        /// Called once between the negotiation step and the upload. It provides
        /// information about what updates will be performed.
        /// </summary>
        public PrePushHandler OnNegotiationCompletedBeforePush { get; set; }

        /// <summary>
        /// Get/Set the custom headers.
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
        /// var pushOptions - new PushOptions() {
        ///     CustomHeaders = new String[] {"X-Request-Id: 12345"}
        /// };
        /// </example>
        /// <value>The custom headers string array</value>
        public string[] CustomHeaders { get; set; }

        /// <summary>
        /// Options for connecting through a proxy.
        /// </summary>
        public ProxyOptions ProxyOptions { get; set; } = new();
    }
}
