using System;
using System.Runtime.InteropServices;
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

        internal unsafe CertificateSsh(git_certificate_ssh* cert)
        {

            HasMD5 = cert->type.HasFlag(GitCertificateSshType.MD5);
            HasSHA1 = cert->type.HasFlag(GitCertificateSshType.SHA1);

            HashMD5 = new byte[16];
            for (var i = 0; i < HashMD5.Length; i++)
            {
                HashMD5[i] = cert->HashMD5[i];
            }

            HashSHA1 = new byte[20];
            for (var i = 0; i < HashSHA1.Length; i++)
            {
                HashSHA1[i] = cert->HashSHA1[i];
            }
        }

        internal unsafe IntPtr ToPointer()
        {
            GitCertificateSshType sshCertType = 0;
            if (HasMD5)
            {
                sshCertType |= GitCertificateSshType.MD5;
            }
            if (HasSHA1)
            {
                sshCertType |= GitCertificateSshType.SHA1;
            }

            var gitCert = new git_certificate_ssh()
            {
                cert_type = GitCertificateType.Hostkey,
                type = sshCertType,
            };

            fixed (byte* p = &HashMD5[0])
            {
                for (var i = 0; i < HashMD5.Length; i++)
                {
                    gitCert.HashMD5[i] = p[i];
                }
            }

            fixed (byte* p = &HashSHA1[0])
            {
                for (var i = 0; i < HashSHA1.Length; i++)
                {
                    gitCert.HashSHA1[i] = p[i];
                }
            }

            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(gitCert));
            Marshal.StructureToPtr(gitCert, ptr, false);

            return ptr;
        }
    }
}
