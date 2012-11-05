using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal delegate int skipped_notify_cb(
		IntPtr skipped_file,
		ref GitOid blob_oid,
		int file_mode,
		IntPtr payload);

    internal delegate void progress_cb(
            IntPtr strPtr,
            UIntPtr completed_steps,
            UIntPtr total_steps,
            IntPtr payload);

    [StructLayout(LayoutKind.Sequential)]
    internal class GitCheckoutOpts
    {
        public CheckoutStrategy checkout_strategy;
        public int DisableFilters;
        public int DirMode;
        public int FileMode;
        public int FileOpenFlags;
        public skipped_notify_cb skippedNotifyCb;
        public IntPtr NotifyPayload;
        public progress_cb ProgressCb;
        public IntPtr ProgressPayload;
        public UnSafeNativeMethods.git_strarray paths;
    }

    [Flags]
    internal enum CheckoutStrategy
    {
        GIT_CHECKOUT_DEFAULT			= (1 << 0),
        GIT_CHECKOUT_OVERWRITE_MODIFIED	= (1 << 1),
        GIT_CHECKOUT_CREATE_MISSING		= (1 << 2),
        GIT_CHECKOUT_REMOVE_UNTRACKED	= (1 << 3),
    }
}
