namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Git certificate types to present to the user
    /// </summary>
    internal enum GitCertificateType
    {
        /// <summary>
        /// No information about the certificate is available.
        /// </summary>
        None = 0,

        /// <summary>
        /// The certificate is a x509 certificate
        /// </summary>
        X509 = 1,

        /// <summary>
        /// The "certificate" is in fact a hostkey identification for ssh.
        /// </summary>
        Hostkey = 2,

        /// <summary>
        /// The "certificate" is in fact a collection of `name:content` strings
        /// containing information about the certificate.
        /// </summary>
        StrArray = 3,
    }
}
