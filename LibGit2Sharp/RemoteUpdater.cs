using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Exposes properties of a remote that can be updated.
    /// </summary>
    public class RemoteUpdater
    {
        private readonly Repository repo;
        private readonly Remote remote;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RemoteUpdater()
        { }

        internal RemoteUpdater(Repository repo, Remote remote)
        {
            Ensure.ArgumentNotNull(repo, "repo");
            Ensure.ArgumentNotNull(remote, "remote");

            this.repo = repo;
            this.remote = remote;
        }

        /// <summary>
        /// Set the default TagFetchMode value for the remote.
        /// </summary>
        public virtual TagFetchMode TagFetchMode
        {
            set
            {
                using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repo.Handle, remote.Name, true))
                {
                    Proxy.git_remote_set_autotag(remoteHandle, value);
                    Proxy.git_remote_save(remoteHandle);
                }
            }
        }
    }
}
