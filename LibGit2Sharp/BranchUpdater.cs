using System;
using LibGit2Sharp.Core;
using System.Globalization;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Exposes properties of a branch that can be updated.
    /// </summary>
    public class BranchUpdater
    {
        private readonly Repository repo;
        private readonly Branch branch;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected BranchUpdater()
        { }

        internal BranchUpdater(Repository repo, Branch branch)
        {
            Ensure.ArgumentNotNull(repo, "repo");
            Ensure.ArgumentNotNull(branch, "branch");

            this.repo = repo;
            this.branch = branch;
        }

        /// <summary>
        ///   Sets the upstream information for the branch.
        ///   <para>
        ///     Passing null or string.Empty will unset the upstream.
        ///   </para>
        /// </summary>
        public virtual string Upstream
        {
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    UnsetUpstream();
                    return;
                }

                SetUpstream(value);
            }
        }

        private void UnsetUpstream()
        {
            repo.Config.Unset(string.Format("branch.{0}.remote", branch.Name));
            repo.Config.Unset(string.Format("branch.{0}.merge", branch.Name));
        }

        /// <summary>
        ///   Set the upstream information for the current branch.
        /// </summary>
        /// <param name="upStreamBranchName">The upstream branch to track.</param>
        private void SetUpstream(string upStreamBranchName)
        {
            if (branch.IsRemote)
            {
                throw new LibGit2SharpException("Cannot set upstream branch on a remote branch.");
            }

            string remoteName;
            string branchName;

            GetUpstreamInformation(upStreamBranchName, out remoteName, out branchName);

            SetUpstreamTo(remoteName, branchName);
        }

        private void SetUpstreamTo(string remoteName, string branchName)
        {
            if (!remoteName.Equals(".", StringComparison.Ordinal))
            {
                // Verify that remote exists.
                repo.Network.Remotes.RemoteForName(remoteName);
            }

            repo.Config.Set(string.Format("branch.{0}.remote", branch.Name), remoteName);
            repo.Config.Set(string.Format("branch.{0}.merge", branch.Name), branchName);
        }

        /// <summary>
        ///   Get the upstream remote and merge branch name from a Canonical branch name.
        ///   This will return the remote name (or ".") if a local branch for the remote name.
        /// </summary>
        /// <param name="canonicalName">The canonical branch name to parse.</param>
        /// <param name="remoteName">The name of the corresponding remote the branch belongs to
        /// or "." if it is a local branch.</param>
        /// <param name="mergeBranchName">The name of the upstream branch to merge into.</param>
        private void GetUpstreamInformation(string canonicalName, out string remoteName, out string mergeBranchName)
        {
            remoteName = null;
            mergeBranchName = null;

            const string localPrefix = "refs/heads/";
            const string remotePrefix = "refs/remotes/";

            if (canonicalName.StartsWith(localPrefix, StringComparison.Ordinal))
            {
                remoteName = ".";
                mergeBranchName = canonicalName;
            }
            else if (canonicalName.StartsWith(remotePrefix, StringComparison.Ordinal))
            {
                remoteName = Proxy.git_branch_remote_name(repo.Handle, canonicalName);

                Remote remote = repo.Network.Remotes.RemoteForName(remoteName);
                using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repo.Handle, remote.Name, true))
                {
                    GitFetchSpecHandle fetchSpecPtr = Proxy.git_remote_fetchspec(remoteHandle);
                    mergeBranchName = Proxy.git_fetchspec_rtransform(fetchSpecPtr, canonicalName);
                }
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' does not look like a valid canonical branch name.", canonicalName));
            }
        }
    }
}
