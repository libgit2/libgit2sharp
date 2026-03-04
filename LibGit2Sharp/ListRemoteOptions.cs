using LibGit2Sharp.Handlers;

namespace LibGit2Sharp;

/// <summary>
/// Options controlling ListRemote behavior.
/// </summary>
public sealed class ListRemoteOptions
{
    /// <summary>
    /// Handler to generate <see cref="LibGit2Sharp.Credentials"/> for authentication.
    /// </summary>
    public CredentialsHandler CredentialsProvider { get; set; }

    /// <summary>
    /// This handler will be called to let the user make a decision on whether to allow
    /// the connection to proceed based on the certificate presented by the server.
    /// </summary>
    public CertificateCheckHandler CertificateCheck { get; set; }


    /// <summary>
    /// Options for connecting through a proxy.
    /// </summary>
    public ProxyOptions ProxyOptions { get; set; } = new();
}
