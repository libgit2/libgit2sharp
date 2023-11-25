using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    public class ProxyOptions
    {
        public ProxyType ProxyType { get; set; }

        public string Url { get; set; }

        public CredentialsHandler CredentialsProvider { get; set; }

        public CertificateCheckHandler CertificateCheck { get; set; }
    }
}
