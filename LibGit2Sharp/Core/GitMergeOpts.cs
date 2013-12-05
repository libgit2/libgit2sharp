using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum MergeFlags
    {
        GIT_MERGE_NO_FASTFORWARD = 1,
        GIT_MERGE_FASTFORWARD_ONLY = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitMergeOpts
    {
        public uint Version;

        public MergeFlags MergeFlags;
        public GitMergeTreeOpts MergeTreeOpts;
        public GitCheckoutOpts CheckoutOpts;
    }
}
