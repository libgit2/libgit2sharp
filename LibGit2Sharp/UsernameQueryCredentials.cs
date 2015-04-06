using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class that holds username query credentials for remote repository access.
    /// </summary>
    public sealed class UsernameQueryCredentials : Credentials
    {
        /// <summary>
        /// Callback to acquire a credential object.
        /// </summary>
        /// <param name="cred">The newly created credential object.</param>
        /// <returns>0 for success, &lt; 0 to indicate an error, &gt; 0 to indicate no credential was acquired.</returns>
        protected internal override int GitCredentialHandler(out IntPtr cred)
        {
            if (Username == null)
            {
                throw new InvalidOperationException("UsernameQueryCredentials contains a null Username.");
            }

            return NativeMethods.git_cred_username_new(out cred, Username);
        }

        /// <summary>
        /// Username for querying the server for supported authentication.
        /// </summary>
        public string Username { get; set; }
    }
}
