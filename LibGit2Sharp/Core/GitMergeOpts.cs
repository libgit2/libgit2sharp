using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitMergeOpts
    {
        public uint Version;

        public GitMergeFlag MergeTreeFlags;

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
        /// Maximum number of times to merge common ancestors to build a
        /// virtual merge base when faced with criss-cross merges.  When this
        /// limit is reached, the next ancestor will simply be used instead of
        /// attempting to merge it.  The default is unlimited.
        /// </summary>
        public uint RecursionLimit;

        /// <summary>
        /// Default merge driver to be used when both sides of a merge have
        /// changed.  The default is the `text` driver.
        /// </summary>
        public string DefaultDriver;

        /// <summary>
        /// Flags for automerging content.
        /// </summary>
        public MergeFileFavor MergeFileFavorFlags;

        /// <summary>
        /// File merging flags.
        /// </summary>
        public GitMergeFileFlag FileFlags;
    }

    /// <summary>
    /// The results of `git_merge_analysis` indicate the merge opportunities.
    /// </summary>
    [Flags]
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

        /// <summary>
        /// The HEAD of the current repository is "unborn" and does not point to
        /// a valid commit.  No merge can be performed, but the caller may wish
        /// to simply set HEAD to the target commit(s).
        /// </summary>
        GIT_MERGE_ANALYSIS_UNBORN = (1 << 3),
    }

    internal enum GitMergePreference
    {
        /// <summary>
        /// No configuration was found that suggests a preferred behavior for
        /// merge.
        /// </summary>
        GIT_MERGE_PREFERENCE_NONE = 0,

        /// <summary>
        /// There is a `merge.ff=false` configuration setting, suggesting that
        /// the user does not want to allow a fast-forward merge.
        /// </summary>
        GIT_MERGE_PREFERENCE_NO_FASTFORWARD = (1 << 0),

        /// <summary>
        /// There is a `merge.ff=only` configuration setting, suggesting that
        /// the user only wants fast-forward merges.
        /// </summary>
        GIT_MERGE_PREFERENCE_FASTFORWARD_ONLY = (1 << 1),
    }

    [Flags]
    internal enum GitMergeFlag
    {
        /// <summary>
        /// No options.
        /// </summary>
        GIT_MERGE_NORMAL = 0,

        /// <summary>
        /// Detect renames that occur between the common ancestor and the "ours"
     	/// side or the common ancestor and the "theirs" side.  This will enable
 	    /// the ability to merge between a modified and renamed file.
        /// </summary>
        GIT_MERGE_FIND_RENAMES = (1 << 0),

        /// <summary>
        /// If a conflict occurs, exit immediately instead of attempting to
        /// continue resolving conflicts.  The merge operation will fail with
        /// GIT_EMERGECONFLICT and no index will be returned.
        ///</summary>
        GIT_MERGE_FAIL_ON_CONFLICT = (1 << 1),

        /// <summary>
        /// Do not write the REUC extension on the generated index
        /// </summary>
        GIT_MERGE_SKIP_REUC = (1 << 2),

        /// <summary>
        /// If the commits being merged have multiple merge bases, do not build
        /// a recursive merge base (by merging the multiple merge bases),
        /// instead simply use the first base.  This flag provides a similar
        /// merge base to `git-merge-resolve`.
        /// </summary>
        GIT_MERGE_NO_RECURSIVE = (1 << 3),
    }

    [Flags]
    internal enum GitMergeFileFlag
    {
        /// <summary>
        /// Defaults
        /// </summary>
        GIT_MERGE_FILE_DEFAULT = 0,

        /// <summary>
        /// Create standard conflicted merge files
        /// </summary>
        GIT_MERGE_FILE_STYLE_MERGE = (1 << 0),

        /// <summary>
        /// Create diff3-style files
        /// </summary>
        GIT_MERGE_FILE_STYLE_DIFF3 = (1 << 1),

        /// <summary>
        /// Condense non-alphanumeric regions for simplified diff file
        /// </summary>
        GIT_MERGE_FILE_SIMPLIFY_ALNUM = (1 << 2),

        /// <summary>
        /// Ignore all whitespace
        /// </summary>
        GIT_MERGE_FILE_IGNORE_WHITESPACE = (1 << 3),

        /// <summary>
        /// Ignore changes in amount of whitespace
        /// </summary>
        GIT_MERGE_FILE_IGNORE_WHITESPACE_CHANGE = (1 << 4),

        /// <summary>
        /// Ignore whitespace at end of line
        /// </summary>
        GIT_MERGE_FILE_IGNORE_WHITESPACE_EOL = (1 << 5),

        /// <summary>
        /// Use the "patience diff" algorithm
        /// </summary>
        GIT_MERGE_FILE_DIFF_PATIENCE = (1 << 6),

        /// <summary>
        /// Take extra time to find minimal diff
        /// </summary>
        GIT_MERGE_FILE_DIFF_MINIMAL = (1 << 7),
    }
}
