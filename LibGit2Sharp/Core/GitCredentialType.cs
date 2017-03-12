using System;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Authentication type requested.
    /// </summary>
    [Flags]
    internal enum GitCredentialType
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

        /// <summary>
        /// TODO
        /// </summary>
        SshInteractive = (1 << 4),

        /// <summary>
        /// Username-only information
        ///
        /// If the SSH transport does not know which username to use,
        /// it will ask via this credential type.
        /// </summary>
        Username = (1 << 5),

        /// <summary>
        /// Credentials read from memory.
        ///
        /// Only available for libssh2+OpenSSL for now.
        /// </summary>
        SshMemory = (1 << 6),
    }
}
