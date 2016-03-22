using System.Collections.Generic;
using LibGit2Sharp;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Commands
{
    /// <summary>
    /// Fetch from a particular remote or the default
    /// </summary>
    public class Fetch
    {
        private readonly Repository repository;
        private readonly string remote;
        private readonly FetchOptions options;
        private readonly string logMessage;
        private readonly IEnumerable<string> refspecs;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2Sharp.Commands.Fetch"/> class.
        /// </summary>
        /// <param name="repository">The repository in which to fetch.</param>
        /// <param name="remote">The remote to fetch from</param>
        /// <param name="options">Fetch options.</param>
        /// <param name="logMessage">Log message for any ref updates.</param>
        /// <param name="refspecs">List of refspecs to apply as active.</param>
        public Fetch(Repository repository, string remote, IEnumerable<string> refspecs, FetchOptions options, string logMessage)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            this.repository = repository;
            this.remote = remote;
            this.options = options ?? new FetchOptions();
            this.logMessage = logMessage;
            this.refspecs = refspecs;
        }

        private RemoteHandle RemoteFromNameOrUrl()
        {
            RemoteHandle handle = null;
            handle = Proxy.git_remote_lookup(repository.Handle, remote, false);

            // If that wasn't the name of a remote, let's use it as a url
            if (handle == null)
            {
                handle = Proxy.git_remote_create_anonymous(repository.Handle, remote);
            }

            return handle;
        }

        /// <summary>
        /// Run this command
        /// </summary>
        public void Run()
        {
            using (var remoteHandle = RemoteFromNameOrUrl())
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
                var fetchOptions = new GitFetchOptions
                {
                    RemoteCallbacks = gitCallbacks,
                    download_tags = Proxy.git_remote_autotag(remoteHandle),
                };

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

                Proxy.git_remote_fetch(remoteHandle, refspecs, fetchOptions, logMessage);
            }

        }
    }
}

