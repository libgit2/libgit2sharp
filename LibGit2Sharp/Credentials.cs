using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class that holds credentials for remote repository access.
    /// </summary>
    public abstract class Credentials
    {
        /// <summary>
        /// Callback to acquire a credential object.
        /// </summary>
        /// <param name="cred">The newly created credential object.</param>
        /// <returns>0 for success, &lt; 0 to indicate an error, &gt; 0 to indicate no credential was acquired.</returns>
        protected internal abstract int GitCredentialHandler(out IntPtr cred);
    }
}
