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
        public Branch Checkout(string shaOrReferenceName)
        {
            // TODO: Allow checkout of an arbitrary commit, thus putting HEAD in detached state.
            // TODO: This does not yet checkout (write) the working directory
            Ensure.ArgumentNotNullOrEmptyString(shaOrReferenceName, "shaOrReferenceName");

            Branch branch = this[shaOrReferenceName];

            EnsureTargetExists(branch, shaOrReferenceName);

            repo.Refs.UpdateTarget("HEAD", branch.CanonicalName);

            return branch;
        }

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "target">The target sha or branch name.</param>
        /// <returns></returns>
        public Branch Create(string name, string target)
        {
            ObjectId id;

            if (!ObjectId.TryParse(target, out id))
            {
                Reference targetRef = repo.Refs[NormalizeToCanonicalName(target)];

                EnsureTargetExists(targetRef, target);
                DirectReference peeledTarget = targetRef.ResolveToDirectReference();
                target = peeledTarget.TargetIdentifier;
            }

            repo.Refs.Create(NormalizeToCanonicalName(name), target);
            return this[name];
        }

        private static void EnsureTargetExists(object target, string identifier)
        {
            if (target != null)
            {
                return;
            }

            throw new LibGit2Exception(String.Format(CultureInfo.InvariantCulture,
                                                     "No commit object identified by '{0}' can be found in the repository.",
                                                     identifier));
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
                throw new LibGit2Exception(string.Format("Branch '{0}' can not be deleted as it is the current HEAD.", canonicalName));
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
        public Branch Move(string currentName, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "name");
            Ensure.ArgumentNotNullOrEmptyString(newName, "name");

            Reference reference = repo.Refs.Move(NormalizeToCanonicalName(currentName), NormalizeToCanonicalName(newName),
                                                 allowOverwrite);

            return this[reference.CanonicalName];
        }

        private static bool LooksLikeABranchName(string referenceName)
        {
            return referenceName.StartsWith("refs/heads/", StringComparison.Ordinal) || referenceName.StartsWith("refs/remotes/", StringComparison.Ordinal);
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