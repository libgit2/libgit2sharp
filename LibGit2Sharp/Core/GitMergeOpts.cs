using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal enum GitMergeFlags
    {
        /// <summary>
        /// Allow fast-forwards,
        /// returning immediately with the commit ID to fast-forward to.
        /// </summary>
        GIT_MERGE_DEFAULT = 0,

        /// <summary>
        /// Do not fast-forward; perform a merge and prepare a merge result even
        /// if the inputs are eligible for fast-forwarding.
        /// </summary>
        GIT_MERGE_NO_FASTFORWARD = 1,

        /// <summary>
        /// Ensure that the inputs are eligible for fast-forwarding,
        /// error if a merge needs to be performed
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
