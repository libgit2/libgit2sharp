using System.Globalization;
using LibGit2Sharp.Core;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides helper overloads to a <see cref = "Network" />.
    /// </summary>
    public static class NetworkExtensions
    {
        /// <summary>
        ///   Push the objectish to the destination reference on the <see cref = "Remote" />.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref = "Remote" /> to push to.</param>
        /// <param name="objectish">The source objectish to push.</param>
        /// <param name="destinationSpec">The reference to update on the remote.</param>
        /// <param name="credentials">Credentials to use for user/pass authentication</param>
        /// <returns>Results of the push operation.</returns>
        public static PushResult Push(
            this Network network,
            Remote remote,
            string objectish,
            string destinationSpec,
            Credentials credentials = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(objectish, "objectish");
            Ensure.ArgumentNotNullOrEmptyString(destinationSpec, "destinationSpec");

            return network.Push(remote, string.Format(CultureInfo.InvariantCulture,
                "{0}:{1}", objectish, destinationSpec), credentials);
        }

        /// <summary>
        ///   Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref = "Remote" /> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        /// <param name="credentials">Credentials to use for user/pass authentication</param>
        /// <returns>Results of the push operation.</returns>
        public static PushResult Push(this Network network, Remote remote, string pushRefSpec, Credentials credentials = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            return network.Push(remote, new string[] { pushRefSpec }, credentials);
        }

        /// <summary>
        ///   Push specified references to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpecs">The pushRefSpecs to push.</param>
        /// <param name="credentials">Credentials to use for user/pass authentication</param>
        /// <returns>Results of the push operation.</returns>
        public static PushResult Push(this Network network, Remote remote, IEnumerable<string> pushRefSpecs, Credentials credentials = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(pushRefSpecs, "pushRefSpecs");

            List<PushStatusError> failedRemoteUpdates = new List<PushStatusError>();

            network.Push(
                remote,
                pushRefSpecs,
                failedRemoteUpdates.Add,
                credentials);

            return new PushResult(failedRemoteUpdates);
        }
    }
}
