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
        ///   <para>
        ///   The upstream branch name is with respect to the current repository.
        ///   So, passing "refs/remotes/origin/master" will set the current branch
        ///   to track "refs/heads/master" on the origin. Passing in
        ///   "refs/heads/master" will result in the branch tracking the local
        ///   master branch.
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

        /// <summary>
        ///   Set the upstream merge branch directly for this branch.
        ///   <para>
        ///   To track the "master" branch on the "origin" remote, set the
        ///   UpstreamRemote property to "origin" and the UpstreamMergeBranch
        ///   property to "refs/heads/master".
        /// </para>
        /// </summary>
        public virtual string UpstreamMergeBranch
        {
            set
            {
                SetUpstreamMergeBranch(value);
            }
        }

        /// <summary>
        ///   Set the upstream remote for this branch.
        ///   <para>
        ///   To track the "master" branch on the "origin" remote, set the
        ///   UpstreamRemote property to "origin" and the UpstreamMergeBranch
        ///   property to "refs/heads/master".
        /// </para>
        /// </summary>
        public virtual string UpstreamRemote
        {
            set
            {
                SetUpstreamRemote(value);
            }
        }

        private void UnsetUpstream()
        {
            SetUpstreamRemote(string.Empty);
            SetUpstreamMergeBranch(string.Empty);
        }

        /// <summary>
        ///   Set the upstream information for the current branch.
        ///   <para>
        ///   The upstream branch name is with respect to the current repository.
        ///   So, passing "refs/remotes/origin/master" will set the current branch
        ///   to track "refs/heads/master" on the origin. Passing in
        ///   "refs/heads/master" will result in the branch tracking the local
        ///   master branch.
        ///   </para>
        ///   </summary>
        /// <param name="upstreamBranchName">The remote branch to track (e.g. refs/remotes/origin/master).</param>
        private void SetUpstream(string upstreamBranchName)
        {
            if (branch.IsRemote)
            {
                throw new LibGit2SharpException("Cannot set upstream branch on a remote branch.");
            }

            string remoteName;
            string branchName;

            GetUpstreamInformation(upstreamBranchName, out remoteName, out branchName);

            SetUpstreamRemote(remoteName);
            SetUpstreamMergeBranch(branchName);
        }

        /// <summary>
        ///   Set the upstream merge branch for the local branch.
        /// </summary>
        /// <param name="mergeBranchName">The merge branch in the upstream remote's namespace.</param>
        private void SetUpstreamMergeBranch(string mergeBranchName)
        {
            string configKey = string.Format("branch.{0}.merge", branch.Name);

            if (string.IsNullOrEmpty(mergeBranchName))
            {
                repo.Config.Unset(configKey);
            }
            else
            {
                repo.Config.Set(configKey, mergeBranchName);
            }
        }

        /// <summary>
        ///   Set the upstream remote for the local branch.
        /// </summary>
        /// <param name="remoteName">The name of the remote to set as the upstream branch.</param>
        private void SetUpstreamRemote(string remoteName)
        {
            string configKey = string.Format("branch.{0}.remote", branch.Name);

            if (string.IsNullOrEmpty(remoteName))
            {
                repo.Config.Unset(configKey);
            }
            else
            {
                if (!remoteName.Equals(".", StringComparison.Ordinal))
                {
                    // Verify that remote exists.
                    repo.Network.Remotes.RemoteForName(remoteName);
                }

                repo.Config.Set(configKey, remoteName);
            }
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
