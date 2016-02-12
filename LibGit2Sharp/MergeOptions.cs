using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Merge behavior.
    /// </summary>
    public sealed class MergeOptions : MergeAndCheckoutOptionsBase
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
        public MergeOptions()
        { }

        /// <summary>
        /// The type of merge to perform.
        /// </summary>
        public FastForwardStrategy FastForwardStrategy { get; set; }
    }

    /// <summary>
    /// Strategy used for merging.
    /// </summary>
    public enum FastForwardStrategy
    {
        /// <summary>
        /// Default fast-forward strategy.  If the merge.ff configuration option is set,
        /// it will be used.  If it is not set, this will perform a fast-forward merge if
        /// possible, otherwise a non-fast-forward merge that results in a merge commit.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Do not fast-forward. Always creates a merge commit.
        /// </summary>
        [Obsolete("This enum member will be removed in the next release. Please use NoFastForward instead.")]
        NoFastFoward = 1, /* GIT_MERGE_NO_FASTFORWARD */

        /// <summary>
        /// Do not fast-forward. Always creates a merge commit.
        /// </summary>
        NoFastForward = 1, /* GIT_MERGE_NO_FASTFORWARD */

        /// <summary>
        /// Only perform fast-forward merges.
        /// </summary>
        FastForwardOnly = 2, /* GIT_MERGE_FASTFORWARD_ONLY */
    }
}
