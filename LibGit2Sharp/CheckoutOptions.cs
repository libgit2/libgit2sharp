using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Collection of parameters controlling Checkout behavior.
    /// </summary>
    public sealed class CheckoutOptions : IConvertableToGitCheckoutOpts
    {
        /// <summary>
        /// Options controlling checkout behavior.
        /// </summary>
        public CheckoutModifiers CheckoutModifiers { get; set; }

        /// <summary>
        /// The flags specifying what conditions are
        /// reported through the OnCheckoutNotify delegate.
        /// </summary>
        public CheckoutNotifyFlags CheckoutNotifyFlags { get; set; }

        /// <summary>
        /// How conflicting index entries should be written out during checkout.
        /// </summary>
        public CheckoutFileConflictStrategy FileConflictStrategy { get; set; }

        /// <summary>
        /// Delegate to be called during checkout for files that match
        /// desired filter specified with the NotifyFlags property.
        /// </summary>
        public CheckoutNotifyHandler OnCheckoutNotify { get; set; }

        /// Delegate through which checkout will notify callers of
        /// certain conditions. The conditions that are reported is
        /// controlled with the CheckoutNotifyFlags property.
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        CheckoutStrategy IConvertableToGitCheckoutOpts.CheckoutStrategy
        {
            get
            {
                var strategy = GitCheckoutOptsWrapper.CheckoutStrategyFromFileConflictStrategy(FileConflictStrategy);
                if (strategy != default(CheckoutStrategy))
                    return CheckoutModifiers.HasFlag(CheckoutModifiers.Force)
                        ? CheckoutStrategy.GIT_CHECKOUT_FORCE | strategy
                        : strategy;

                if (CheckoutModifiers.HasFlag(CheckoutModifiers.Force))
                    return CheckoutStrategy.GIT_CHECKOUT_FORCE;

                return CheckoutStrategy.GIT_CHECKOUT_SAFE;
            }
        }

        /// <summary>
        /// Generate a <see cref="CheckoutCallbacks"/> object with the delegates
        /// hooked up to the native callbacks.
        /// </summary>
        /// <returns></returns>
        CheckoutCallbacks IConvertableToGitCheckoutOpts.GenerateCallbacks()
        {
            return CheckoutCallbacks.From(OnCheckoutProgress, OnCheckoutNotify);
        }
    }
}
