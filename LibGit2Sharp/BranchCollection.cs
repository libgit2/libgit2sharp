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
        public Branch this[string name]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(name, "name");
                string canonicalName = NormalizeToCanonicalName(name);
                var reference = repo.Refs.Resolve<Reference>(canonicalName);
                return reference == null ? null : new Branch(repo, reference, canonicalName);
            }
        }

        #region IEnumerable<Branch> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Branch> GetEnumerator()
        {
            return Libgit2UnsafeHelper.ListAllReferenceNames(repo.Handle, GitReferenceType.ListAll)
                .Where(LooksLikeABranchName)
                .Select(n => this[n])
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
        ///   Checkout the branch with the specified by name.
        /// </summary>
        /// <param name = "shaOrReferenceName">The sha of the commit, a canonical reference name or the name of the branch to checkout.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in the next release. Please use Repository.Checkout() instead.")]
        public IBranch Checkout(string shaOrReferenceName)
        {
            return repo.Checkout(shaOrReferenceName);
        }

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "shaOrReferenceName">The target which can be sha or a canonical reference name.</param>
        /// <returns></returns>
        public IBranch Create(string name, string shaOrReferenceName)
        {
            ObjectId commitId = repo.LookupCommit(shaOrReferenceName).Id;

            repo.Refs.Create(NormalizeToCanonicalName(name), commitId.Sha);
            return this[name];
        }

        /// <summary>
        ///   Deletes the branch with the specified name.
        /// </summary>
        /// <param name = "name">The name of the branch to delete.</param>
        public void Delete(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            string canonicalName = NormalizeToCanonicalName(name);

            if (canonicalName == repo.Head.CanonicalName)
            {
                throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture, "Branch '{0}' can not be deleted as it is the current HEAD.", canonicalName));
            }

            //TODO: To be replaced by native libgit2 git_branch_delete() when available.
            repo.Refs.Delete(canonicalName);
        }

        ///<summary>
        ///  Rename an existing branch with a new name.
        ///</summary>
        ///<param name = "currentName">The current branch name.</param>
        ///<param name = "newName">The new name of the existing branch should bear.</param>
        ///<param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        ///<returns></returns>
        public IBranch Move(string currentName, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "name");
            Ensure.ArgumentNotNullOrEmptyString(newName, "name");

            Reference reference = repo.Refs.Move(NormalizeToCanonicalName(currentName),
                                                 NormalizeToCanonicalName(newName),
                                                 allowOverwrite);

            return this[reference.CanonicalName];
        }

        private static bool LooksLikeABranchName(string referenceName)
        {
            return referenceName.StartsWith("refs/heads/", StringComparison.Ordinal) ||
                   referenceName.StartsWith("refs/remotes/", StringComparison.Ordinal);
        }

        private static string NormalizeToCanonicalName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (name == "HEAD")
            {
                return name;
            }

            if (LooksLikeABranchName(name))
            {
                return name;
            }

            return string.Format(CultureInfo.InvariantCulture, "refs/heads/{0}", name);
        }
    }
}
