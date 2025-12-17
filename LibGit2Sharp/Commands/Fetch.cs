using System.Collections.Generic;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class to serve as namespacing for the command-emulating methods
    /// </summary>
    public static partial class Commands
    {
        private static RemoteHandle RemoteFromNameOrUrl(RepositoryHandle repoHandle, string remote)
        {
            RemoteHandle handle = null;
            handle = Proxy.git_remote_lookup(repoHandle, remote, false);

            // If that wasn't the name of a remote, let's use it as a url
            if (handle == null)
            {
                handle = Proxy.git_remote_create_anonymous(repoHandle, remote);
            }

            return handle;
        }

        /// <summary>
        /// Perform a fetch
        /// </summary>
        /// <param name="repository">The repository in which to fetch.</param>
        /// <param name="remote">The remote to fetch from. Either as a remote name or a URL</param>
        /// <param name="options">Fetch options.</param>
        /// <param name="logMessage">Log message for any ref updates.</param>
        /// <param name="refspecs">List of refspecs to apply as active.</param>
        public static void Fetch(Repository repository, string remote, IEnumerable<string> refspecs, FetchOptions options, string logMessage)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            options = options ?? new FetchOptions();
            using (var remoteHandle = RemoteFromNameOrUrl(repository.Handle, remote))
            using (var fetchOptionsWrapper = new GitFetchOptionsWrapper())
            {

                var callbacks = new RemoteCallbacks(options);
                GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();

                // It is OK to pass the reference to the GitCallbacks directly here because libgit2 makes a copy of
                // the data in the git_remote_callbacks structure. If, in the future, libgit2 changes its implementation
                // to store a reference to the git_remote_callbacks structure this would introduce a subtle bug
                // where the managed layer could move the git_remote_callbacks to a different location in memory,
                // but libgit2 would still reference the old address.
                //
                // Also, if GitRemoteCallbacks were a class instead of a struct, we would need to guard against
                // GC occuring in between setting the remote callbacks and actual usage in one of the functions afterwords.
                var fetchOptions = fetchOptionsWrapper.Options;
                fetchOptions.RemoteCallbacks = gitCallbacks;
                fetchOptions.download_tags = Proxy.git_remote_autotag(remoteHandle);

                if (options.TagFetchMode.HasValue)
                {
                    fetchOptions.download_tags = options.TagFetchMode.Value;
                }

                if (options.Prune.HasValue)
                {
                    fetchOptions.Prune = options.Prune.Value ? FetchPruneStrategy.Prune : FetchPruneStrategy.NoPrune;
                }
                else
                {
                    fetchOptions.Prune = FetchPruneStrategy.FromConfigurationOrDefault;
                }

                if (options.CustomHeaders != null && options.CustomHeaders.Length > 0)
                {
                    fetchOptions.CustomHeaders = GitStrArrayManaged.BuildFrom(options.CustomHeaders);
                }

                fetchOptions.ProxyOptions = options.ProxyOptions.CreateGitProxyOptions();

                Proxy.git_remote_fetch(remoteHandle, refspecs, fetchOptions, logMessage);
            }

        }
    }
}

