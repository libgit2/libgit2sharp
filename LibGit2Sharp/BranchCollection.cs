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
    ///   The collection of Branches in a <see cref = "Repository" />
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class BranchCollection : IEnumerable<Branch>
    {
        internal readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected BranchCollection()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "BranchCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal BranchCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Branch" /> with the specified name.
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
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", "refs/heads/", name);
        }

        private static string ShortToRemoteName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", "refs/remotes/", name);
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
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Branch> GetEnumerator()
        {
            return Proxy.git_branch_foreach(repo.Handle, GitBranchType.GIT_BRANCH_LOCAL | GitBranchType.GIT_BRANCH_REMOTE, (b, t) => Utf8Marshaler.FromNative(b))
                .Select(n => this[n])
                .OrderBy(b => b.CanonicalName, StringComparer.Ordinal)
                .GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "commit">The target commit.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Add(string name, Commit commit, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(commit, "commit");

            using (Proxy.git_branch_create(repo.Handle, name, commit.Id, allowOverwrite)) {}

            return this[ShortToLocalName(name)];
        }

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "committish">Revparse spec for the target commit.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Branch Create(string name, string committish, bool allowOverwrite = false)
        {
            return this.Add(name, committish, allowOverwrite);
        }

        /// <summary>
        ///   Deletes the specified branch.
        /// </summary>
        /// <param name = "branch">The branch to delete.</param>
        public virtual void Remove(Branch branch)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            using (ReferenceSafeHandle referencePtr = repo.Refs.RetrieveReferencePtr(branch.CanonicalName))
            {
                Proxy.git_branch_delete(referencePtr);
            }
        }

        /// <summary>
        ///   Deletes the branch with the specified name.
        /// </summary>
        /// <param name = "name">The name of the branch to delete.</param>
        /// <param name = "isRemote">True if the provided <paramref name="name"/> is the name of a remote branch, false otherwise.</param>
        [Obsolete("This method will be removed in the next release. Please use Remove() instead.")]
        public virtual void Delete(string name, bool isRemote = false)
        {
            this.Remove(name, isRemote);
        }

        /// <summary>
        ///   Renames an existing local branch with a new name.
        /// </summary>
        /// <param name = "branch">The current local branch.</param>
        /// <param name = "newName">The new name the existing branch should bear.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
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

            using (ReferenceSafeHandle referencePtr = repo.Refs.RetrieveReferencePtr("refs/heads/" + branch.Name))
            {
                Proxy.git_branch_move(referencePtr, newName, allowOverwrite);
            }

            return this[newName];
        }

        private static bool LooksLikeABranchName(string referenceName)
        {
            return referenceName == "HEAD" ||
                referenceName.StartsWith("refs/heads/", StringComparison.Ordinal) ||
                referenceName.StartsWith("refs/remotes/", StringComparison.Ordinal);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", this.Count());
            }
        }
    }
}
