using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of Branches in a <see cref="Repository"/>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class BranchCollection : IEnumerable<Branch>
    {
        internal readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected BranchCollection()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BranchCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        internal BranchCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        /// Gets the <see cref="LibGit2Sharp.Branch"/> with the specified name.
        /// </summary>
        public virtual Branch this[string name]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(name, "name");

                if (LooksLikeABranchName(name))
                {
                    return BuildFromReferenceName(name);
                }

                Branch branch = BuildFromReferenceName(ShortToLocalName(name));
                if (branch != null)
                {
                    return branch;
                }

                branch = BuildFromReferenceName(ShortToRemoteName(name));
                if (branch != null)
                {
                    return branch;
                }

                return BuildFromReferenceName(ShortToRefName(name));
            }
        }

        private static string ShortToLocalName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", Reference.LocalBranchPrefix, name);
        }

        private static string ShortToRemoteName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", Reference.RemoteTrackingBranchPrefix, name);
        }

        private static string ShortToRefName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", "refs/", name);
        }

        private Branch BuildFromReferenceName(string canonicalName)
        {
            var reference = repo.Refs.Resolve<Reference>(canonicalName);
            return reference == null ? null : new Branch(repo, reference, canonicalName);
        }

        #region IEnumerable<Branch> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Branch> GetEnumerator()
        {
            return Proxy.git_branch_foreach(repo.Handle, GitBranchType.GIT_BRANCH_LOCAL | GitBranchType.GIT_BRANCH_REMOTE, branchToCanoncialName)
                .Select(n => this[n])
                .GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Create a new local branch with the specified name
        /// </summary>
        /// <param name="name">The name of the branch.</param>
        /// <param name="commit">The target commit.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Add(string name, Commit commit, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(commit, "commit");

            return Add(name, commit, allowOverwrite, "branch: Created from " + commit.Id);
        }

        internal Branch Add(string name, Commit commit, bool allowOverwrite, string logMessage)
        {
            Ensure.ArgumentNotNull(commit, "commit");

            using (Proxy.git_branch_create(repo.Handle, name, commit.Id, allowOverwrite)) {}

            var branch = this[ShortToLocalName(name)];
            LogBranch(branch, logMessage);
            return branch;
        }

        /// <summary>
        /// Deletes the specified branch.
        /// </summary>
        /// <param name="branch">The branch to delete.</param>
        public virtual void Remove(Branch branch)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            using (ReferenceSafeHandle referencePtr = repo.Refs.RetrieveReferencePtr(branch.CanonicalName))
            {
                Proxy.git_branch_delete(referencePtr);
            }
        }

        /// <summary>
        /// Renames an existing local branch with a new name.
        /// </summary>
        /// <param name="branch">The current local branch.</param>
        /// <param name="newName">The new name the existing branch should bear.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Move(Branch branch, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNull(branch, "branch");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            if (branch.IsRemote)
            {
                throw new LibGit2SharpException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Cannot rename branch '{0}'. It's a remote tracking branch.", branch.Name));
            }

            using (ReferenceSafeHandle referencePtr = repo.Refs.RetrieveReferencePtr(Reference.LocalBranchPrefix + branch.Name))
            {
                using (ReferenceSafeHandle ref_out = Proxy.git_branch_move(referencePtr, newName, allowOverwrite))
                {
                }
            }

            var newBranch = this[newName];
            LogBranch(newBranch, "Branch: renamed " + branch.CanonicalName + " to " + newBranch.CanonicalName);
            return newBranch;
        }

        /// <summary>
        /// Update properties of a branch.
        /// </summary>
        /// <param name="branch">The branch to update.</param>
        /// <param name="actions">Delegate to perform updates on the branch.</param>
        /// <returns>The updated branch.</returns>
        public virtual Branch Update(Branch branch, params Action<BranchUpdater>[] actions)
        {
            var updater = new BranchUpdater(repo, branch);

            foreach (Action<BranchUpdater> action in actions)
            {
                action(updater);
            }

            return this[branch.Name];
        }

        private static bool LooksLikeABranchName(string referenceName)
        {
            return referenceName == "HEAD" ||
                referenceName.LooksLikeLocalBranch() ||
                referenceName.LooksLikeRemoteTrackingBranch();
        }

        private static string branchToCanoncialName(IntPtr namePtr, GitBranchType branchType)
        {
            string shortName = Utf8Marshaler.FromNative(namePtr);

            switch (branchType)
            {
                case GitBranchType.GIT_BRANCH_LOCAL:
                    return ShortToLocalName(shortName);
                case GitBranchType.GIT_BRANCH_REMOTE:
                    return ShortToRemoteName(shortName);
                default:
                    return shortName;
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", this.Count());
            }
        }

        private void LogBranch(Branch branch, string logMessage)
        {
            if (string.IsNullOrEmpty(logMessage))
            {
                return;
            }

            repo.Refs.Log(branch.CanonicalName).Append(branch.Tip.Id, logMessage, branch.Tip.Committer);
        }
    }
}
