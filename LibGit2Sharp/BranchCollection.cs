using System;
using System.Collections;
using System.Collections.Generic;
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
        public BranchCollection(Repository repo)
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

        public IEnumerator<Branch> GetEnumerator()
        {
            return Libgit2UnsafeHelper.ListAllReferenceNames(repo.Handle, GitReferenceType.ListAll)
                .Where(LooksLikeABranchName)
                .Select(n => this[n])
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "target">The target sha or branch name.</param>
        /// <returns></returns>
        public Branch Create(string name, string target)
        {
            ObjectId id = ObjectId.CreateFromMaybeSha(target);
            if (id != null)
            {
                return Create(name, id);
            }

            repo.Refs.Create(NormalizeToCanonicalName(name), NormalizeToCanonicalName(target));

            return this[name];
        }

        /// <summary>
        ///   Create a new local branch with the specified name.
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "target">The target.</param>
        /// <returns></returns>
        public Branch Create(string name, ObjectId target)
        {
            Ensure.ArgumentNotNull(target, "target");

            repo.Refs.Create(NormalizeToCanonicalName(name), target);

            return this[name];
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

            return string.Format("refs/heads/{0}", name);
        }
    }
}