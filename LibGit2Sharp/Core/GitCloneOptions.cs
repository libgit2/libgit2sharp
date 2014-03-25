using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitCloneOptions
    {
        public uint Version;

        public GitCheckoutOpts CheckoutOpts;
        public GitRemoteCallbacks RemoteCallbacks;

        public int Bare;
        public int IgnoreCertErrors;

        public IntPtr RemoteName;
        public IntPtr CheckoutBranch;
        public IntPtr signature; // Really a SignatureSafeHandle
    }
}
