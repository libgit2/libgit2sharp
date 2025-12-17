using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal enum GitProxyType
    {
        None,
        Auto,
        Specified
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitProxyOptions
    {
        public uint Version;
        public GitProxyType Type;
        public IntPtr Url;
        public NativeMethods.git_cred_acquire_cb Credentials;
        public NativeMethods.git_transport_certificate_check_cb CertificateCheck;
        public IntPtr Payload;
    }
}
