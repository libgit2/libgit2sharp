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
    }
}
