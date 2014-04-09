using System;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Authentication type requested.
    /// </summary>
    [Flags]
    public enum GitCredentialType
    {
        /// <summary>
        /// A plaintext username and password.
        /// </summary>
        UserPassPlaintext = (1 << 0),

        /// <summary>
        /// A ssh key from disk.
        /// </summary>
        SshKey = (1 << 1),

        /// <summary>
        /// A key with a custom signature function.
        /// </summary>
        SshCustom = (1 << 2),

        /// <summary>
        /// A key for NTLM/Kerberos "default" credentials.
        /// </summary>
        Default = (1 << 3),
    }
}

