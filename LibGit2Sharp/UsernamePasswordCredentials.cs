using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class that holds username and password credentials for remote repository access.
    /// </summary>
    public sealed class UsernamePasswordCredentials : Credentials
    {
        /// <summary>
        /// Callback to acquire a credential object.
        /// </summary>
        /// <param name="cred">The newly created credential object.</param>
        /// <returns>0 for success, &lt; 0 to indicate an error, &gt; 0 to indicate no credential was acquired.</returns>
        protected internal override int GitCredentialHandler(out IntPtr cred)
        {
            if (Username == null || Password == null)
            {
                throw new InvalidOperationException("UsernamePasswordCredentials contains a null Username or Password.");
            }

            return NativeMethods.git_cred_userpass_plaintext_new(out cred, Username, Password);
        }

        /// <summary>
        /// Username for username/password authentication (as in HTTP basic auth).
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password for username/password authentication (as in HTTP basic auth).
        /// </summary>
        public string Password { get; set; }
    }
}
