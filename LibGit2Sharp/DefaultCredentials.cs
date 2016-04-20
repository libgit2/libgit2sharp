using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A credential object that will provide the "default" credentials
    /// (logged-in user information) via NTLM or SPNEGO authentication.
    /// </summary>
    public sealed class DefaultCredentials : Credentials
    {
        /// <summary>
        /// Callback to acquire a credential object.
        /// </summary>
        /// <param name="cred">The newly created credential object.</param>
        /// <returns>0 for success, &lt; 0 to indicate an error, &gt; 0 to indicate no credential was acquired.</returns>
        protected internal override int GitCredentialHandler(out IntPtr cred)
        {
            return NativeMethods.git_cred_default_new(out cred);
        }
    }
}
