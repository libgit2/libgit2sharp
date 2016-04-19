using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_certificate_x509
    {
        /// <summary>
        /// Type of the certificate, in this case, GitCertificateType.X509
        /// </summary>
        public GitCertificateType cert_type;
        /// <summary>
        /// Pointer to the X509 certificate data
        /// </summary>
        public byte* data;
        /// <summary>
        ///  The size of the certificate data
        /// </summary>
        public UIntPtr len;
    }
}
