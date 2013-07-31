using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="Network"/>.
    /// </summary>
    public static class NetworkExtensions
    {
        /// <summary>
        /// Push the specified branch to its tracked branch on the remote.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="branch">The branch to push.</param>
        /// <param name="onPushStatusError">Handler for reporting failed push updates.</param>
        /// <param name="credentials">Credentials to use for user/pass authentication.</param>
        /// <exception cref="LibGit2SharpException">Throws if either the Remote or the UpstreamBranchCanonicalName is not set.</exception>
        public static void Push(
            this Network network,
            Branch branch,
            PushStatusErrorHandler onPushStatusError = null,
            Credentials credentials = null)
        {
            network.Push(new[] { branch }, onPushStatusError, credentials);
        }

        /// <summary>
        /// Push the specified branches to their tracked branches on the remote.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="branches">The branches to push.</param>
        /// <param name="onPushStatusError">Handler for reporting failed push updates.</param>
        /// <param name="credentials">Credentials to use for user/pass authentication.</param>
        /// <exception cref="LibGit2SharpException">Throws if either the Remote or the UpstreamBranchCanonicalName is not set.</exception>
        public static void Push(
            this Network network,
            IEnumerable<Branch> branches,
            PushStatusErrorHandler onPushStatusError = null,
            Credentials credentials = null)
        {
            var enumeratedBranches = branches as IList<Branch> ?? branches.ToList();

            foreach (var branch in enumeratedBranches)
            {
                if (string.IsNullOrEmpty(branch.UpstreamBranchCanonicalName))
                {
                    throw new LibGit2SharpException(string.Format("The branch '{0}' (\"{1}\") that you are trying to push does not track an upstream branch.",
                                                                  branch.Name, branch.CanonicalName));
                }
            }

            foreach (var branch in enumeratedBranches)
            {
                network.Push(branch.Remote, string.Format("{0}:{1}", branch.CanonicalName, branch.UpstreamBranchCanonicalName), onPushStatusError, credentials);
            }
        }

        /// <summary>
        /// Push the objectish to the destination reference on the <see cref="Remote"/>.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
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
        /// Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="network">The <see cref="Network"/> being worked with.</param>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        /// <param name="credentials">Credentials to use for user/pass authentication</param>
        /// <returns>Results of the push operation.</returns>
        public static PushResult Push(this Network network, Remote remote, string pushRefSpec, Credentials credentials = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            return network.Push(remote, new[] { pushRefSpec }, credentials);
        }

        /// <summary>
        /// Push specified references to the <see cref="Remote"/>.
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

            var failedRemoteUpdates = new List<PushStatusError>();

            network.Push(
                remote,
                pushRefSpecs,
                failedRemoteUpdates.Add,
                credentials);

            return new PushResult(failedRemoteUpdates);
        }
    }
}
