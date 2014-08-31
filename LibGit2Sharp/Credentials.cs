using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

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
        /// <param name="url">The resource for which we are demanding a credential.</param>
        /// <param name="usernameFromUrl">The username that was embedded in a "user@host"</param>
        /// <param name="types">A bitmask stating which cred types are OK to return.</param>
        /// <param name="payload">The payload provided when specifying this callback.</param>
        /// <returns>0 for success, &lt; 0 to indicate an error, &gt; 0 to indicate no credential was acquired.</returns>
        protected internal abstract int GitCredentialHandler(out IntPtr cred, IntPtr url, IntPtr usernameFromUrl, GitCredentialType types, IntPtr payload);
    }

    internal interface ICredentialsProvider
    {
        /// <summary>
        /// Handler to generate <see cref="LibGit2Sharp.Credentials"/> for authentication.
        /// </summary>
        CredentialsHandler CredentialsProvider { get; }
    }

    internal static class CredentialsProviderExtensions
    {
        public static CredentialsHandler GetCredentialsHandler(this ICredentialsProvider provider)
        {
            if (provider == null)
            {
                return null;
            }

            if (provider.CredentialsProvider == null)
            {
                return null;
            }

            return provider.CredentialsProvider;
        }
    }
}
