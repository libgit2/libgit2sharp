using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitMergeOpts
    {
        public uint Version;

        public GitMergeTreeFlags MergeTreeFlags;

        /// <summary>
        /// Similarity to consider a file renamed.
        /// </summary>
        public uint RenameThreshold;

        /// <summary>
        /// Maximum similarity sources to examine (overrides
        /// 'merge.renameLimit' config (default 200)
        /// </summary>
        public uint TargetLimit;

        /// <summary>
        /// Pluggable similarityMetric; pass IntPtr.Zero
        /// to use internal metric.
        /// </summary>
        public IntPtr SimilarityMetric;

        /// <summary>
        /// Flags for automerging content.
        /// </summary>
        public GitMergeFileFavorFlags MergeFileFavorFlags;
    }

	/// <summary>
	/// The results of `git_merge_analysis` indicate the merge opportunities.
	/// </summary>
    internal enum GitMergeAnalysis
    {
        /// <summary>
        /// No merge is possible.  (Unused.)
        /// </summary>
        GIT_MERGE_ANALYSIS_NONE = 0,

        /// <summary>
	    /// A "normal" merge; both HEAD and the given merge input have diverged
	    /// from their common ancestor.  The divergent commits must be merged.
        /// </summary>
        GIT_MERGE_ANALYSIS_NORMAL = (1 << 0),

        /// <summary>
        /// All given merge inputs are reachable from HEAD, meaning the
        /// repository is up-to-date and no merge needs to be performed.
        /// </summary>
        GIT_MERGE_ANALYSIS_UP_TO_DATE = (1 << 1),

        /// <summary>
        /// The given merge input is a fast-forward from HEAD and no merge
        /// needs to be performed.  Instead, the client can check out the
        /// given merge input.
        /// </summary>
        GIT_MERGE_ANALYSIS_FASTFORWARD = (1 << 2),

        /**
         * The HEAD of the current repository is "unborn" and does not point to
         * a valid commit.  No merge can be performed, but the caller may wish
         * to simply set HEAD to the target commit(s).
         */
        GIT_MERGE_ANALYSIS_UNBORN = (1 << 3),
    }

    [Flags]
    internal enum GitMergeTreeFlags
    {
        /// <summary>
        /// No options.
        /// </summary>
        GIT_MERGE_TREE_NORMAL = 0,

        /// <summary>
        /// GIT_MERGE_TREE_FIND_RENAMES in libgit2
        /// </summary>
        GIT_MERGE_TREE_FIND_RENAMES = (1 << 0),
    }

    internal enum GitMergeFileFavorFlags
    {
        GIT_MERGE_FILE_FAVOR_NORMAL = 0,
        GIT_MERGE_FILE_FAVOR_OURS = 1,
        GIT_MERGE_FILE_FAVOR_THEIRS = 2,
        GIT_MERGE_FILE_FAVOR_UNION = 3,
    }
}
