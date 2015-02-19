using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling rebase behavior.
    /// </summary>
    public sealed class RebaseOptions : IConvertableToGitCheckoutOpts
    {
        /// <summary>
        /// Delegate that is called before each rebase step.
        /// </summary>
        public RebaseStepStartingHandler RebaseStepStarting { get; set; }

        /// <summary>
        /// Delegate that is called after each rebase step is completed.
        /// </summary>
        public RebaseStepCompletedHandler RebaseStepCompleted { get; set; }

        /// <summary>
        /// The Flags specifying what conditions are
        /// reported through the OnCheckoutNotify delegate.
        /// </summary>
        public CheckoutNotifyFlags CheckoutNotifyFlags { get; set; }

        /// <summary>
        /// Delegate that the checkout will report progress through.
        /// </summary>
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// Delegate that checkout will notify callers of
        /// certain conditions. The conditions that are reported is
        /// controlled with the CheckoutNotifyFlags property.
        /// </summary>
        public CheckoutNotifyHandler OnCheckoutNotify { get; set; }

        /// <summary>
        /// How conflicting index entries should be written out during checkout.
        /// </summary>
        public CheckoutFileConflictStrategy FileConflictStrategy { get; set; }

        CheckoutCallbacks IConvertableToGitCheckoutOpts.GenerateCallbacks()
        {
            return CheckoutCallbacks.From(OnCheckoutProgress, OnCheckoutNotify);
        }

        CheckoutStrategy IConvertableToGitCheckoutOpts.CheckoutStrategy
        {
            get
            {
                return CheckoutStrategy.GIT_CHECKOUT_SAFE |
                       GitCheckoutOptsWrapper.CheckoutStrategyFromFileConflictStrategy(FileConflictStrategy);
            }
        }
    }
}
