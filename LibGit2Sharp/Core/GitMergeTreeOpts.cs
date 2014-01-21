using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitMergeTreeOpts
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
}
