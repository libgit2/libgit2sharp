using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options for connecting through a proxy.
    /// </summary>
    public sealed class ProxyOptions
    {
        /// <summary>
        /// The type of proxy to use. Set to Auto by default.
        /// </summary>
        public ProxyType ProxyType { get; set; } = ProxyType.Auto;

        /// <summary>
        /// The URL of the proxy when <see cref="LibGit2Sharp.ProxyType"/> is set to Specified.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Handler to generate <see cref="LibGit2Sharp.Credentials"/> for authentication.
        /// </summary>
        public CredentialsHandler CredentialsProvider { get; set; }

        /// <summary>
        /// This handler will be called to let the user make a decision on whether to allow
        /// the connection to proceed based on the certificate presented by the server.
        /// </summary>
        public CertificateCheckHandler CertificateCheck { get; set; }

        internal unsafe GitProxyOptions CreateGitProxyOptions()
        {
            var gitProxyOptions = new GitProxyOptions
            {
                Version = 1,
                Type = (GitProxyType)ProxyType
            };

            if (Url is not null)
            {
                gitProxyOptions.Url = StrictUtf8Marshaler.FromManaged(Url);
            }

            if (CredentialsProvider is not null)
            {
                gitProxyOptions.Credentials = GitCredentialHandler;
            }

            if (CertificateCheck is not null)
            {
                gitProxyOptions.CertificateCheck = GitCertificateCheck;
            }

            return gitProxyOptions;
        }

        private int GitCredentialHandler(out IntPtr ptr, IntPtr cUrl, IntPtr usernameFromUrl, GitCredentialType credTypes, IntPtr payload)
        {
            string url = LaxUtf8Marshaler.FromNative(cUrl);
            string username = LaxUtf8Marshaler.FromNative(usernameFromUrl);

            SupportedCredentialTypes types = default(SupportedCredentialTypes);
            if (credTypes.HasFlag(GitCredentialType.UserPassPlaintext))
            {
                types |= SupportedCredentialTypes.UsernamePassword;
            }
            if (credTypes.HasFlag(GitCredentialType.Default))
            {
                types |= SupportedCredentialTypes.Default;
            }

            ptr = IntPtr.Zero;
            try
            {
                var cred = CredentialsProvider(url, username, types);
                if (cred == null)
                {
                    return (int)GitErrorCode.PassThrough;
                }
                return cred.GitCredentialHandler(out ptr);
            }
            catch (Exception exception)
            {
                Proxy.git_error_set_str(GitErrorCategory.Callback, exception);
                return (int)GitErrorCode.Error;
            }
        }

        private unsafe int GitCertificateCheck(git_certificate* certPtr, int valid, IntPtr cHostname, IntPtr payload)
        {
            string hostname = LaxUtf8Marshaler.FromNative(cHostname);
            Certificate cert = null;

            switch (certPtr->type)
            {
                case GitCertificateType.X509:
                    cert = new CertificateX509((git_certificate_x509*)certPtr);
                    break;
                case GitCertificateType.Hostkey:
                    cert = new CertificateSsh((git_certificate_ssh*)certPtr);
                    break;
            }

            bool result = false;
            try
            {
                result = CertificateCheck(cert, valid != 0, hostname);
            }
            catch (Exception exception)
            {
                Proxy.git_error_set_str(GitErrorCategory.Callback, exception);
            }

            return Proxy.ConvertResultToCancelFlag(result);
        }
    }
}
