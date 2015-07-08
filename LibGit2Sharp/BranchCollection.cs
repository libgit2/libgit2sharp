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
            return Proxy.git_branch_iterator(repo, GitBranchType.GIT_BRANCH_ALL)
                        .ToList()
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
        /// <param name="committish">Revparse spec for the target commit.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Add(string name, string committish)
        {
            return Add(name, committish, false);
        }

        /// <summary>
        /// Create a new local branch with the specified name
        /// </summary>
        /// <param name="name">The name of the branch.</param>
        /// <param name="commit">The target commit.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Add(string name, Commit commit)
        {
            return Add(name, commit, false);
        }

        /// <summary>
        /// Create a new local branch with the specified name
        /// </summary>
        /// <param name="name">The name of the branch.</param>
        /// <param name="commit">The target commit.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Add(string name, Commit commit, bool allowOverwrite)
        {
            Ensure.ArgumentNotNull(commit, "commit");

            return Add(name, commit.Sha, allowOverwrite);
        }

        /// <summary>
        /// Create a new local branch with the specified name
        /// </summary>
        /// <param name="name">The name of the branch.</param>
        /// <param name="committish">Revparse spec for the target commit.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Add(string name, string committish, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            using (Proxy.git_branch_create_from_annotated(repo.Handle, name, committish, allowOverwrite))
            { }

            var branch = this[ShortToLocalName(name)];
            return branch;
        }

        /// <summary>
        /// Deletes the branch with the specified name.
        /// </summary>
        /// <param name="name">The name of the branch to delete.</param>
        public virtual void Remove(string name)
        {
            Remove(name, false);
        }

        /// <summary>
        /// Deletes the branch with the specified name.
        /// </summary>
        /// <param name="name">The name of the branch to delete.</param>
        /// <param name="isRemote">True if the provided <paramref name="name"/> is the name of a remote branch, false otherwise.</param>
        public virtual void Remove(string name, bool isRemote)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            string branchName = isRemote ? Reference.RemoteTrackingBranchPrefix + name : name;

            Branch branch = this[branchName];

            if (branch == null)
            {
                return;
            }

            Remove(branch);
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
        /// Rename an existing local branch, using the default reflog message
        /// </summary>
        /// <param name="currentName">The current branch name.</param>
        /// <param name="newName">The new name the existing branch should bear.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Rename(string currentName, string newName)
        {
            return Rename(currentName, newName, false);
        }

        /// <summary>
        /// Rename an existing local branch, using the default reflog message
        /// </summary>
        /// <param name="currentName">The current branch name.</param>
        /// <param name="newName">The new name the existing branch should bear.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Rename(string currentName, string newName, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "currentName");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            Branch branch = this[currentName];

            if (branch == null)
            {
                throw new LibGit2SharpException("No branch named '{0}' exists in the repository.");
            }

            return Rename(branch, newName, allowOverwrite);
        }

        /// <summary>
        /// Rename an existing local branch
        /// </summary>
        /// <param name="branch">The current local branch.</param>
        /// <param name="newName">The new name the existing branch should bear.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Rename(Branch branch, string newName)
        {
            return Rename(branch, newName, false);
        }

        /// <summary>
        /// Rename an existing local branch
        /// </summary>
        /// <param name="branch">The current local branch.</param>
        /// <param name="newName">The new name the existing branch should bear.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Rename(Branch branch, string newName, bool allowOverwrite)
        {
            Ensure.ArgumentNotNull(branch, "branch");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            if (branch.IsRemote)
            {
                throw new LibGit2SharpException(CultureInfo.InvariantCulture,
                                                "Cannot rename branch '{0}'. It's a remote tracking branch.",
                                                branch.FriendlyName);
            }

            using (ReferenceSafeHandle referencePtr = repo.Refs.RetrieveReferencePtr(Reference.LocalBranchPrefix + branch.FriendlyName))
            {
                using (Proxy.git_branch_move(referencePtr, newName, allowOverwrite))
                { }
            }

            var newBranch = this[newName];
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

            return this[branch.FriendlyName];
        }

        private static bool LooksLikeABranchName(string referenceName)
        {
            return referenceName == "HEAD" ||
                referenceName.LooksLikeLocalBranch() ||
                referenceName.LooksLikeRemoteTrackingBranch();
        }

        private string DebuggerDisplay
        {
            get { return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count()); }
        }
    }
}
