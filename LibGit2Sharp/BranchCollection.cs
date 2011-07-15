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
            get { return repo.Refs.Resolve<Branch>(NormalizeToCanonicalName(name)); }
        }

        #region IEnumerable<Branch> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Branch> GetEnumerator()
        {
            return Libgit2UnsafeHelper.ListAllReferenceNames(repo.Handle, GitReferenceType.ListAll)
                .Where(LooksLikeABranchName)
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
        ///   Checkout the branch with the specified by name.
        /// </summary>
        /// <param name = "name">The name of the branch to checkout.</param>
        /// <returns></returns>
        public Branch Checkout(string name)
        {
            // TODO: Allow checkout of an arbitrary commit, thus putting HEAD in detached state.
            // TODO: This does not yet checkout (write) the working directory
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            repo.Refs.UpdateTarget("HEAD", this[name].CanonicalName);

            return this[name];
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
                var reference = repo.Refs[NormalizeToCanonicalName(target)].ResolveToDirectReference();
                target = reference.TargetIdentifier;
            }

            repo.Refs.Create(NormalizeToCanonicalName(name), target);
            return this[name];
        }

        /// <summary>
        ///   Deletes the branch with the specified name.
        /// </summary>
        /// <param name = "name">The name of the branch to delete.</param>
        public void Delete(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            repo.Refs.Delete(NormalizeToCanonicalName(name)); //TODO: To be replaced by native libgit2 git_branch_delete() when available.
        }

        ///<summary>
        /// Rename an existing branch with a new name.
        ///</summary>
        ///<param name="currentName">The current branch name.</param>
        ///<param name="newName">The new name of the existing branch should bear.</param>
        ///<param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
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

            if (LooksLikeABranchName(name))
            {
                return name;
            }

            return string.Format(CultureInfo.InvariantCulture, "refs/heads/{0}", name);
        }
    }
}