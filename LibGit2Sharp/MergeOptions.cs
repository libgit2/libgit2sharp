
namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Merge behavior.
    /// </summary>
    public class MergeOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeOptions"/> class.
        /// By default, a fast-forward merge will be performed if possible, and
        /// if a merge commit is created, then it will be commited.
        /// </summary>
        public MergeOptions()
        {
            CommitOnSuccess = true;
        }

        /// <summary>
        /// Commit the merge if the merge is successful and this is a non-fast-forward merge.
        /// If this is a fast-forward merge, then there is no merge commit and this option
        /// will not affect the merge.
        /// </summary>
        public virtual bool CommitOnSuccess { get; set; }

        /// <summary>
        /// The type of merge to perform.
        /// </summary>
        public virtual FastForwardStrategy FastForwardStrategy { get; set; }
    }

    /// <summary>
    /// Strategy used for merging.
    /// </summary>
    public enum FastForwardStrategy
    {
        /// <summary>
        /// Default fast-forward strategy. This will perform a fast-forward merge
        /// if possible, otherwise will perform a non-fast-forward merge that
        /// results in a merge commit.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Do not fast-forward. Always creates a merge commit.
        /// </summary>
        NoFastFoward = 1, /* GIT_MERGE_NO_FASTFORWARD */

        /// <summary>
        /// Only perform fast-forward merges.
        /// </summary>
        FastForwardOnly = 2, /* GIT_MERGE_FASTFORWARD_ONLY */
    }
}
