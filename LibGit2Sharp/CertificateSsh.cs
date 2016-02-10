using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// This class represents the hostkey which is avaiable when connecting to a SSH host.
    /// </summary>
    public class CertificateSsh : Certificate
    {
        /// <summary>
        /// For mocking purposes
        /// </summary>
        protected CertificateSsh()
        { }

        /// <summary>
        /// The MD5 hash of the host. Meaningful if <see cref="HasMD5"/> is true
        /// </summary>
        public readonly byte[] HashMD5;

        /// <summary>
        /// The SHA1 hash of the host. Meaningful if <see cref="HasSHA1"/> is true
        /// </summary>
        public readonly byte[] HashSHA1;

        /// <summary>
        /// True if we have the MD5 hostkey hash from the server
        /// </summary>
        public readonly bool HasMD5;

        /// <summary>
        /// True if we have the SHA1 hostkey hash from the server
        /// </summary>
        public readonly bool HasSHA1;

        /// <summary>
        /// True if we have the SHA1 hostkey hash from the server
        /// </summary>public readonly bool HasSHA1;

        internal CertificateSsh(GitCertificateSsh cert)
        {

            HasMD5  = cert.type.HasFlag(GitCertificateSshType.MD5);
            HasSHA1 = cert.type.HasFlag(GitCertificateSshType.SHA1);

            HashMD5 = new byte[16];
            cert.HashMD5.CopyTo(HashMD5, 0);

            HashSHA1 = new byte[20];
            cert.HashSHA1.CopyTo(HashSHA1, 0);
        }
    }
}
