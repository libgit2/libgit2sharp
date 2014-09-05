using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Revert behavior.
    /// </summary>
    public sealed class RevertOptions : IConvertableToGitCheckoutOpts
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RevertOptions"/> class.
        /// By default the revert will be committed if there are no conflicts.
        /// </summary>
        public RevertOptions()
        {
            CommitOnSuccess = true;

            FindRenames = true;

            // TODO: libgit2 should provide reasonable defaults for these
            //       values, but it currently does not.
            RenameThreshold = 50;
            TargetLimit = 200;
        }

        /// <summary>
        /// The Flags specifying what conditions are
        /// reported through the OnCheckoutNotify delegate.
        /// </summary>
        public CheckoutNotifyFlags CheckoutNotifyFlags { get; set; }

        /// <summary>
        /// Delegate that checkout progress will be reported through.
        /// </summary>
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// Delegate that checkout will notify callers of
        /// certain conditions. The conditions that are reported is
        /// controlled with the CheckoutNotifyFlags property.
        /// </summary>
        public CheckoutNotifyHandler OnCheckoutNotify { get; set; }

        /// <summary>
        /// Commit changes if there are no conflicts and the revert results
        /// in changes.
        /// <para>
        ///  Following command line behavior, if the revert results in no
        ///  changes, then Revert will cleanup the repository state if
        ///  <see cref="CommitOnSuccess"/> is true (i.e. the repository
        ///  will not be left in a "revert in progress" state).
        ///  If <see cref="CommitOnSuccess"/> is false and there are no
        ///  changes to revert, then the repository will be left in
        ///  the "revert in progress" state.
        /// </para>
        /// </summary>
        public bool CommitOnSuccess { get; set; }

        /// <summary>
        /// When reverting a merge commit, the parent number to consider as
        /// mainline, starting from offset 1.
        /// <para>
        ///  As a merge commit has multiple parents, reverting a merge commit
        ///  will reverse all the changes brought in by the merge except for
        ///  one parent's line of commits. The parent to preserve is called the
        ///  mainline, and must be specified by its number (i.e. offset).
        /// </para>
        /// </summary>
        public int Mainline { get; set; }

        /// <summary>
        /// How to handle conflicts encountered during a merge.
        /// </summary>
        public MergeFileFavor MergeFileFavor { get; set; }

        /// <summary>
        /// How Checkout should handle writing out conflicting index entries.
        /// </summary>
        public CheckoutFileConflictStrategy FileConflictStrategy { get; set; }

        /// <summary>
        /// Find renames. Default is true.
        /// </summary>
        public bool FindRenames { get; set; }

        /// <summary>
        /// Similarity to consider a file renamed (default 50). If
        /// `FindRenames` is enabled, added files will be compared
        /// with deleted files to determine their similarity. Files that are
        /// more similar than the rename threshold (percentage-wise) will be
        /// treated as a rename.
        /// </summary>
        public int RenameThreshold;

        /// <summary>
        /// Maximum similarity sources to examine for renames (default 200).
        /// If the number of rename candidates (add / delete pairs) is greater
        /// than this value, inexact rename detection is aborted.
        ///
        /// This setting overrides the `merge.renameLimit` configuration value.
        /// </summary>
        public int TargetLimit;

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

        #endregion IConvertableToGitCheckoutOpts

    }
}
