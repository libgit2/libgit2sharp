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
