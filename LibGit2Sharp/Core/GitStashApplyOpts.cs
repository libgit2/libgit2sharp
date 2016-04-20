using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal delegate int stash_apply_progress_cb(StashApplyProgress progress, IntPtr payload);

    [StructLayout(LayoutKind.Sequential)]
    internal class GitStashApplyOpts
    {
        public uint Version = 1;

        public StashApplyModifiers Flags;
        public GitCheckoutOpts CheckoutOptions;

        public stash_apply_progress_cb ApplyProgressCallback;
        public IntPtr ProgressPayload;
    }
}
