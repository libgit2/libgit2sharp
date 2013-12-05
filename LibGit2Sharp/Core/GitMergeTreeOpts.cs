using LibGit2Sharp.Core.Handles;
using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum MergeTreeFlags
    {
	    GIT_MERGE_TREE_FIND_RENAMES = (1 << 0),
    }
    [Flags]
    internal enum MergeAutomergeFlags
    {
	    GIT_MERGE_AUTOMERGE_NORMAL = 0,
	    GIT_MERGE_AUTOMERGE_NONE = 1,
	    GIT_MERGE_AUTOMERGE_FAVOR_OURS = 2,
	    GIT_MERGE_AUTOMERGE_FAVOR_THEIRS = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitMergeTreeOpts
    {
        public uint Version;

        public MergeTreeFlags MergeTreeFlags;
        public uint RenameThreshold;
        public uint TargetLimit;

        public UIntPtr Metric;

        public MergeAutomergeFlags MergeAutomergeFlags;
    }
}
