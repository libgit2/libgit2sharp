using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitCloneOptions
    {
        public uint Version = 1;

        public GitCheckoutOpts CheckoutOpts;
        public int Bare;
        public NativeMethods.git_transfer_progress_callback TransferProgressCallback;
        public IntPtr TransferProgressPayload;

        public IntPtr RemoteName;
        public IntPtr PushUrl;
        public IntPtr FetchSpec;
        public IntPtr PushSpec;

        public IntPtr CredAcquireCallback;
        public IntPtr CredAcquirePayload;

        public IntPtr Transport;
        public GitRemoteCallbacks RemoteCallbacks;
        public int RemoteAutotag;
    }
}
