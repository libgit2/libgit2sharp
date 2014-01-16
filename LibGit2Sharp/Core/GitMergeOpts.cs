using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal enum GitMergeFlags
    {
        /// <summary>
        /// Default
        /// </summary>
        GIT_MERGE_DEFAULT = 0,

        /// <summary>
        /// Do not fast-forward.
        /// </summary>
        GIT_MERGE_NO_FASTFORWARD = 1,

        /// <summary>
        /// Only perform fast-forward.
        /// </summary>
        GIT_MERGE_FASTFORWARD_ONLY = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitMergeOpts
    {
        public uint Version;

        public GitMergeFlags MergeFlags;
        public GitMergeTreeOpts MergeTreeOpts;
        public GitCheckoutOpts CheckoutOpts;
    }
}
