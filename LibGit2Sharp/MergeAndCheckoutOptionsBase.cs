using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling the behavior of things that do a merge and then
    /// check out the merge results (eg: merge, revert, cherry-pick).
    /// </summary>
    public abstract class MergeAndCheckoutOptionsBase : MergeOptionsBase, IConvertableToGitCheckoutOpts
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeOptions"/> class.
        /// <para>
        ///   Default behavior:
        ///     A fast-forward merge will be performed if possible, unless the merge.ff configuration option is set.
        ///     A merge commit will be committed, if one was created.
        ///     Merge will attempt to find renames.
        /// </para>
        /// </summary>
        public MergeAndCheckoutOptionsBase()
        {
            CommitOnSuccess = true;
        }

        /// <summary>
        /// The Flags specifying what conditions are
        /// reported through the OnCheckoutNotify delegate.
        /// </summary>
        public CheckoutNotifyFlags CheckoutNotifyFlags { get; set; }

        /// <summary>
        /// Commit the merge if the merge is successful and this is a non-fast-forward merge.
        /// If this is a fast-forward merge, then there is no merge commit and this option
        /// will not affect the merge.
        /// </summary>
        public bool CommitOnSuccess { get; set; }

        /// <summary>
        /// How conflicting index entries should be written out during checkout.
        /// </summary>
        public CheckoutFileConflictStrategy FileConflictStrategy { get; set; }

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

        #region IConvertableToGitCheckoutOpts

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

        #endregion
    }
}
