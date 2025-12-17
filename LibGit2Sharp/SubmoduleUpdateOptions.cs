using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Submodule Update behavior and callbacks.
    /// </summary>
    public sealed class SubmoduleUpdateOptions : IConvertableToGitCheckoutOpts
    {
        /// <summary>
        /// Initialize the submodule if it is not already initialized.
        /// </summary>
        public bool Init { get; set; }

        /// <summary>
        /// Delegate to be called during checkout for files that match
        /// desired filter specified with the NotifyFlags property.
        /// </summary>
        public CheckoutNotifyHandler OnCheckoutNotify { get; set; }

        /// Delegate through which checkout will notify callers of
        /// certain conditions. The conditions that are reported is
        /// controlled with the CheckoutNotifyFlags property.
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// The flags specifying what conditions are
        /// reported through the OnCheckoutNotify delegate.
        /// </summary>
        public CheckoutNotifyFlags CheckoutNotifyFlags { get; set; }

        /// <summary>
        /// Collection of parameters controlling Fetch behavior.
        /// </summary>
        public FetchOptions FetchOptions { get; internal set; } = new();

        CheckoutCallbacks IConvertableToGitCheckoutOpts.GenerateCallbacks()
        {
            return CheckoutCallbacks.From(OnCheckoutProgress, OnCheckoutNotify);
        }

        CheckoutStrategy IConvertableToGitCheckoutOpts.CheckoutStrategy
        {
            get { return CheckoutStrategy.GIT_CHECKOUT_SAFE; }
        }

        CheckoutNotifyFlags IConvertableToGitCheckoutOpts.CheckoutNotifyFlags
        {
            get { return CheckoutNotifyFlags; }
        }
    }
}
