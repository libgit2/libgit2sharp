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

namespace LibGit2Sharp
{
    /// <summary>
    ///   A branch is a special kind of reference
    /// </summary>
    public class Branch
    {
        private readonly Repository repo;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Branch" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal Branch(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the name of the remote (null for local branches).
        /// </summary>
        public string RemoteName { get; private set; }

        /// <summary>
        ///   Gets the name of this branch.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///   Gets the reference for this branch.
        /// </summary>
        public DirectReference Reference { get; private set; }

        /// <summary>
        ///   Gets the commits on this branch. (Starts walking from the References's target).
        /// </summary>
        public CommitCollection Commits
        {
            get { return repo.Commits.StartingAt(this); }
        }

        /// <summary>
        ///   Gets the type of this branch.
        /// </summary>
        public BranchType Type { get; private set; }

        internal static Branch CreateBranchFromReference(Reference reference, Repository repo)
        {
            var tokens = reference.Name.Split('/');
            if (tokens.Length < 2)
            {
                throw new ArgumentException(string.Format("Unexpected ref name: {0}", reference.Name));
            }

            if (tokens[tokens.Length - 2] == "heads")
            {
                return new Branch(repo)
                           {
                               Name = tokens[tokens.Length - 1],
                               Reference = (DirectReference) reference,
                               Type = BranchType.Local
                           };
            }
            return new Branch(repo)
                       {
                           Name = string.Join("/", tokens, tokens.Length - 2, 2),
                           RemoteName = tokens[tokens.Length - 2],
                           Reference = (DirectReference) reference,
                           Type = BranchType.Remote
                       };
        }
    }
}