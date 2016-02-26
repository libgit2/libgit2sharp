﻿using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class that holds SSH username with key credentials for remote repository access.
    /// </summary>
    public sealed class SshUserKeyCredentials : Credentials
    {
        /// <summary>
        /// Callback to acquire a credential object.
        /// </summary>
        /// <param name="cred">The newly created credential object.</param>
        /// <returns>0 for success, &lt; 0 to indicate an error, &gt; 0 to indicate no credential was acquired.</returns>
        protected internal override int GitCredentialHandler(out IntPtr cred)
        {
            if (!GlobalSettings.Version.Features.HasFlag(BuiltInFeatures.Ssh))
            {
                throw new InvalidOperationException("LibGit2 was not built with SSH support.");
            }

            if (Username == null)
            {
                throw new InvalidOperationException("SshUserKeyCredentials contains a null Username.");
            }

            if (Passphrase == null)
            {
                throw new InvalidOperationException("SshUserKeyCredentials contains a null Passphrase.");
            }

            if (PublicKey == null)
            {
                throw new InvalidOperationException("SshUserKeyCredentials contains a null PublicKey.");
            }

            if (PrivateKey == null)
            {
                throw new InvalidOperationException("SshUserKeyCredentials contains a null PrivateKey.");
            }

            return NativeMethods.git_cred_ssh_key_new(out cred, Username, PublicKey, PrivateKey, Passphrase);
        }

        /// <summary>
        /// Username for SSH authentication.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Public key file location for SSH authentication.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Private key file location for SSH authentication.
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Passphrase for SSH authentication.
        /// </summary>
        public string Passphrase { get; set; }
    }
}
