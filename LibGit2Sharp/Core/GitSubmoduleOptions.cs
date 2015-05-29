using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitSubmoduleOptions
    {
        public uint Version;

        public GitCheckoutOpts CheckoutOptions;

        public GitFetchOptions FetchOptions;

        public CheckoutStrategy CloneCheckoutStrategy;
    }
}
