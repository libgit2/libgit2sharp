using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
            get
            {
                var tokens = name.Split('/');
                if (tokens.Length == 1)
                {
                    return Branch.CreateBranchFromReference(repo.Refs[string.Format(CultureInfo.InvariantCulture, "refs/heads/{0}", name)], repo);
                }
                if (tokens.Length == 2)
                {
                    return Branch.CreateBranchFromReference(repo.Refs[string.Format(CultureInfo.InvariantCulture, "refs/{0}", name)], repo);
                }
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unable to parse branch name: {0}. Expecting local branches in the form <branchname> and remotes in the form <remote>/<branchname>.", name));
            }
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

        private static bool IsABranch(Reference reference)
        {
            return reference.Type == GitReferenceType.Oid
                   && !reference.Name.StartsWith("refs/tags/");
        }
    }
}