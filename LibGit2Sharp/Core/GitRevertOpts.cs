using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitRevertOpts
    {
        public uint Version = 1;

        // For merge commits, the "mainline" is treated as the parent
        public uint Mainline = 0;

        public GitMergeTreeOpts MergeTreeOpts = new GitMergeTreeOpts {Version = 1};

        public GitCheckoutOpts CheckoutOpts = new GitCheckoutOpts {version = 1};
    }
}
