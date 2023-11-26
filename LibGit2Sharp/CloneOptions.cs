using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options to define clone behaviour
    /// </summary>
    public sealed class CloneOptions : IConvertableToGitCheckoutOpts
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
        /// The name of the branch to checkout. When unspecified the
        /// remote's default branch will be used instead.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Recursively clone submodules.
        /// </summary>
        public bool RecurseSubmodules { get; set; }

        /// <summary>
        /// Handler for checkout progress information.
        /// </summary>
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// Gets or sets the fetch options.
        /// </summary>
        public FetchOptions FetchOptions { get; } = new();

        #region IConvertableToGitCheckoutOpts

        CheckoutCallbacks IConvertableToGitCheckoutOpts.GenerateCallbacks()
        {
            return CheckoutCallbacks.From(OnCheckoutProgress, null);
        }

        CheckoutStrategy IConvertableToGitCheckoutOpts.CheckoutStrategy
        {
            get
            {
                return this.Checkout
                    ? CheckoutStrategy.GIT_CHECKOUT_SAFE
                    : CheckoutStrategy.GIT_CHECKOUT_NONE;
            }
        }

        CheckoutNotifyFlags IConvertableToGitCheckoutOpts.CheckoutNotifyFlags
        {
            get { return CheckoutNotifyFlags.None; }
        }

        #endregion
    }
}
