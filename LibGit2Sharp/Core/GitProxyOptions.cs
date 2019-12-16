using System;
using System.Runtime.InteropServices;
using System.Text;

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
        public IntPtr CredentialsCb;
        public IntPtr CertificateCheck;
        public IntPtr CbPayload;
    }

    internal static class GitProxyOptionsFactory
    {
        internal static GitProxyOptions CreateGitProxyOptions(ProxyOptions proxyOptions)
        {
            var options = new GitProxyOptions
            {
                Version = 1,
                Type = GitProxyType.None,
            };

            if (proxyOptions != null)
            {
                options.Type = (GitProxyType)proxyOptions.ProxyType;

                if (!string.IsNullOrWhiteSpace(proxyOptions.Url))
                {
                    options.Url = EncodingMarshaler.FromManaged(Encoding.UTF8, proxyOptions.Url);
                }
            }

            return options;
        }
    }
}
