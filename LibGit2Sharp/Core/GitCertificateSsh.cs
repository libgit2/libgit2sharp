using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitCertificateSsh
    {
        public GitCertificateType cert_type;
        public GitCertificateSshType type;

        /// <summary>
        /// The MD5 hash (if appropriate)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] HashMD5;

        /// <summary>
        /// The MD5 hash (if appropriate)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] HashSHA1;
    }
}
