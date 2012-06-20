using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of Branches in a <see cref = "Repository" />
    /// </summary>
    public class BranchCollection : IEnumerable<Branch>
    {
        private readonly Repository repo;

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
            return Libgit2UnsafeHelper
                .ListAllBranchNames(repo.Handle, GitBranchType.GIT_BRANCH_LOCAL | GitBranchType.GIT_BRANCH_REMOTE)
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
        /// <param name = "shaOrReferenceName">The target which can be sha or a canonical reference name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns></returns>
        public virtual Branch Add(string name, string shaOrReferenceName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            ObjectId commitId = repo.LookupCommit(shaOrReferenceName).Id;

            using (var osw = new ObjectSafeWrapper(commitId, repo))
            {
                GitOid oid;
                Ensure.Success(NativeMethods.git_branch_create(out oid, repo.Handle, name, osw.ObjectPtr, allowOverwrite));
            }

            return this[ShortToLocalName(name)];
        }

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "shaOrReferenceName">The target which can be sha or a canonical reference name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Branch Create(string name, string shaOrReferenceName, bool allowOverwrite = false)
        {
            return Add(name, shaOrReferenceName, allowOverwrite);
        }

        /// <summary>
        ///   Deletes the branch with the specified name.
        /// </summary>
        /// <param name = "name">The name of the branch to delete.</param>
        /// <param name = "isRemote">True if the provided <paramref name="name"/> is the name of a remote branch, false otherwise.</param>
        public virtual void Remove(string name, bool isRemote = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            int res = NativeMethods.git_branch_delete(repo.Handle, name, isRemote ? GitBranchType.GIT_BRANCH_REMOTE : GitBranchType.GIT_BRANCH_LOCAL);

            if (res == (int)GitErrorCode.NotFound)
            {
                return;
            }

            Ensure.Success(res);
        }

        /// <summary>
        ///   Deletes the branch with the specified name.
        /// </summary>
        /// <param name = "name">The name of the branch to delete.</param>
        /// <param name = "isRemote">True if the provided <paramref name="name"/> is the name of a remote branch, false otherwise.</param>
        [Obsolete("This method will be removed in the next release. Please use Remove() instead.")]
        public virtual void Delete(string name, bool isRemote = false)
        {
            Remove(name, isRemote);
        }

        ///<summary>
        ///  Rename an existing local branch with a new name.
        ///</summary>
        ///<param name = "currentName">The current branch name.</param>
        ///<param name = "newName">The new name of the existing branch should bear.</param>
        ///<param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        ///<returns></returns>
        public virtual Branch Move(string currentName, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "currentName");
            Ensure.ArgumentNotNullOrEmptyString(newName, "name");

            Ensure.Success(NativeMethods.git_branch_move(repo.Handle, currentName, newName, allowOverwrite));

            return this[newName];
        }

        private static bool LooksLikeABranchName(string referenceName)
        {
            return referenceName == "HEAD" ||
                referenceName.StartsWith("refs/heads/", StringComparison.Ordinal) ||
                referenceName.StartsWith("refs/remotes/", StringComparison.Ordinal);
        }
    }
}
