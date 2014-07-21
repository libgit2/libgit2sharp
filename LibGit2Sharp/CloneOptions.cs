using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options to define clone behaviour
    /// </summary>
    public sealed class CloneOptions : IConvertableToGitCheckoutOpts, ICredentialsProvider
    {
        /// <summary>
        /// Creates default <see cref="CloneOptions"/> for a non-bare clone
        /// </summary>
        public CloneOptions()
        {
            Checkout = true;
            OnRemoteCreation = DefaultRemoteCreationHandler;
        }

        /// <summary>
        /// True will result in a bare clone, false a full clone.
        /// </summary>
        public bool IsBare { get; set; }

        /// <summary>
        /// If true, the origin's HEAD will be checked out. This only applies
        /// to non-bare repositories.
        /// </summary>
        public bool Checkout { get; set; }

        /// <summary>
        /// Handler for network transfer and indexing progress information
        /// </summary>
        public TransferProgressHandler OnTransferProgress { get; set; }

        /// <summary>
        /// Handler for checkout progress information
        /// </summary>
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// Credentials to use for user/pass authentication
        /// </summary>
        [Obsolete("This will be removed in future release. Use CredentialsProvider.")]
        public Credentials Credentials { get; set; }

        /// <summary>
        /// Handler to generate <see cref="LibGit2Sharp.Credentials"/> for authentication.
        /// </summary>
        public CredentialsHandler CredentialsProvider { get; set; }

        /// <summary>
        /// Handler to use to create the remote.
        /// </summary>
        public RemoteCreationHandler OnRemoteCreation { get; set; }

        /// <summary>
        /// Default remote creation handler for the CloneOptions.
        /// </summary>
        /// <param name="repo">The repository where the remote should be created</param>
        /// <param name="name">The suggested name of the remote</param>
        /// <param name="url">The suggested URL of the remote</param>
        /// <returns>The created remote</returns>
        public Remote DefaultRemoteCreationHandler(Repository repo, string name, string url)
        {
            // Use the suggested remote name (generally "origin") and URI.
            Remote remote = repo.Network.Remotes.Add(name, url);

            // TODO: The caller ought to be able to invoke any or all of the following here:
            //   git_remote_set_transport
            //   git_remote_check_cert
            //   git_remote_set_callbacks
            //
            // These all represent volatile properties of a git_remote instance, and are not
            // serialized back to the "store" of remotes.

            return remote;
        }

        #region IConvertableToGitCheckoutOpts

        CheckoutCallbacks IConvertableToGitCheckoutOpts.GenerateCallbacks()
        {
            return CheckoutCallbacks.From(OnCheckoutProgress, null);
        }

        CheckoutStrategy IConvertableToGitCheckoutOpts.CheckoutStrategy
        {
            get
            {
                return this.Checkout ?
                    CheckoutStrategy.GIT_CHECKOUT_SAFE_CREATE :
                    CheckoutStrategy.GIT_CHECKOUT_NONE;
            }
        }

        CheckoutNotifyFlags IConvertableToGitCheckoutOpts.CheckoutNotifyFlags
        {
            get { return CheckoutNotifyFlags.None; }
        }

        #endregion
    }
}
