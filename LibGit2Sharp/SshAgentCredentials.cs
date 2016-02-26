﻿using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class that holds SSH agent credentials for remote repository access.
    /// </summary>
    public sealed class SshAgentCredentials : Credentials
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
                throw new InvalidOperationException("SshAgentCredentials contains a null Username.");
            }

            return NativeMethods.git_cred_ssh_key_from_agent(out cred, Username);
        }

        /// <summary>
        /// Username for SSH authentication.
        /// </summary>
        public string Username { get; set; }
    }
}
