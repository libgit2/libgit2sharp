using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Credential types supported by the server. If the server supports a particular type of
    /// authentication, it will be set to true.
    /// </summary>
    [Flags]
    public enum SupportedCredentialTypes
    {
        /// <summary>
        /// Plain username and password combination
        /// </summary>
        UsernamePassword = (1 << 0),

        /// <summary>
        /// Ask Windows to provide its default credentials for the current user (e.g. NTLM)
        /// </summary>
        Default = (1 << 1),

        /// <summary>
        /// SSH with username and public/private keys. (SshUserKeyCredentials, SshAgentCredentials).
        /// </summary>
        Ssh = (1 << 2),

        /// <summary>
        /// Queries the server with the given username, then later returns the supported credential types.
        /// </summary>
        UsernameQuery = (1 << 3),
    }
}
