using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitCloneOptions
    {
        public uint Version = 1;

        public GitCheckoutOpts CheckoutOpts;
        public GitRemoteCallbacks RemoteCallbacks;

        public int Bare;
        public int IgnoreCertErrors;

        public IntPtr RemoteName;
        public IntPtr CheckoutBranch;
    }
}
