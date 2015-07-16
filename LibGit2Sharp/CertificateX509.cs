using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Conains a X509 certificate
    /// </summary>
    public class CertificateX509 : Certificate
    {

        /// <summary>
        /// For mocking purposes
        /// </summary>
        protected CertificateX509()
        { }

        /// <summary>
        /// The certificate.
        /// </summary>
        public virtual X509Certificate Certificate { get; private set; }

        internal CertificateX509(GitCertificateX509 cert)
        {
            int len = checked((int) cert.len.ToUInt32());
            byte[] data = new byte[len];
            Marshal.Copy(cert.data, data, 0, len);
            Certificate = new X509Certificate(data);
        }
    }
}
