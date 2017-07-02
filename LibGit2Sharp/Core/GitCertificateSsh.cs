using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_certificate_ssh
    {
        public GitCertificateType cert_type;
        public GitCertificateSshType type;

        /// <summary>
        /// The MD5 hash (if appropriate)
        /// </summary>
        public unsafe fixed byte HashMD5[16];

        /// <summary>
        /// The SHA1 hash (if appropriate)
        /// </summary>
        public unsafe fixed byte HashSHA1[20];
    }
}
