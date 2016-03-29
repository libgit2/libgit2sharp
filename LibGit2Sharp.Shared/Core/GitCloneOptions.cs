using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal enum GitCloneLocal
    {
        CloneLocalAuto,
        CloneLocal,
        CloneNoLocal,
        CloneLocalNoLinks
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitCloneOptions
    {
        public uint Version;

        public GitCheckoutOpts CheckoutOpts;
        public GitFetchOptions FetchOpts;

        public int Bare;
        public GitCloneLocal Local;
        public IntPtr CheckoutBranch;

        public IntPtr signature; // Really a SignatureSafeHandle

        public IntPtr RepositoryCb;
        public IntPtr RepositoryCbPayload;

        public IntPtr RemoteCb;
        public IntPtr RemoteCbPayload;
    }
}
