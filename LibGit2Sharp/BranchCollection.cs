#region  Copyright (c) 2011 LibGit2Sharp committers

//  The MIT License
//  
//  Copyright (c) 2011 LibGit2Sharp committers
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.

#endregion

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
                    return Branch.CreateBranchFromReference(repo.Refs[string.Format(CultureInfo.InvariantCulture, "refs/heads/{0}", name)]);
                }
                if (tokens.Length == 2)
                {
                    return Branch.CreateBranchFromReference(repo.Refs[string.Format(CultureInfo.InvariantCulture, "refs/{0}", name)]);
                }
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unable to parse branch name: {0}. Expecting local branches in the form <branchname> and remotes in the form <remote>/<branchname>.", name));
            }
        }

        #region IEnumerable<Branch> Members

        public IEnumerator<Branch> GetEnumerator()
        {
            var list = repo.Refs
                .Where(IsABranch)
                .Select(Branch.CreateBranchFromReference);
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