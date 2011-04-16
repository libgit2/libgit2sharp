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
        public BranchCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Branch" /> with the specified name.
        /// </summary>
        public Branch this[string name]
        {
            get { return Branch.CreateBranchFromReference(repo.Refs[ParseName(name)], repo); }
        }

        #region IEnumerable<Branch> Members

        public IEnumerator<Branch> GetEnumerator()
        {
            var list = repo.Refs
                .Where(IsABranch)
                .Select(p => Branch.CreateBranchFromReference(p, repo));
            return list.GetEnumerator();
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
        /// <param name = "target">The target sha, ref or branch name.</param>
        /// <returns></returns>
        public Branch Create(string name, string target)
        {
            ObjectId id = ObjectId.CreateFromMaybeSha(target);
            if(id != null)
            {
                return Create(name, id);
            }

            var reference = repo.Refs.Create(EnsureValidBranchName(name), ParseName(target));
            return Branch.CreateBranchFromReference(reference, repo);
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

            var reference = repo.Refs.Create(EnsureValidBranchName(name), target);
            return Branch.CreateBranchFromReference(reference, repo);
        }

        private static string EnsureValidBranchName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (name.Contains("/"))
            {
                throw new ArgumentException("Branch names cannot contain the character '/'.");
            }

            return string.Format(CultureInfo.InvariantCulture, "refs/heads/{0}", name);
        }

        private static bool IsABranch(Reference reference)
        {
            return /*reference.Type == GitReferenceType.Oid
                   &&*/ !reference.CanonicalName.StartsWith("refs/tags/");
        }

        private static string ParseName(string name)
        {
            var tokens = name.Split('/');
            if (tokens.Length == 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "refs/heads/{0}", name);
            }
            if (tokens.Length == 2)
            {
                return string.Format(CultureInfo.InvariantCulture, "refs/{0}", name);
            }
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unable to parse branch name: {0}. Expecting local branches in the form <branchname> and remotes in the form <remote>/<branchname>.", name));
        }
    }
}