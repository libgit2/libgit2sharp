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
using System.Collections.Generic;

namespace LibGit2Sharp
{
    public class Commit : GitObject
    {
        //public Commit(Repository repo, Signature author, Signature committer, string message = null, Tree tree = null, IEnumerable<Commit> parents = null, string updateRefName = null)
        //{
        //}

        private Signature author;
        private Signature committer;
        private string message;
        private string messageShort;
        private List<Commit> parents;

        internal Commit(IntPtr obj, GitOid? oid = null)
            : base(obj, oid)
        {
        }

        public string Message
        {
            get { return message ?? (message = NativeMethods.git_commit_message(Obj)); }
        }

        public string MessageShort
        {
            get { return messageShort ?? (messageShort = NativeMethods.git_commit_message_short(Obj)); }
        }

        public Signature Author
        {
            get { return author ?? (author = new Signature(NativeMethods.git_commit_author(Obj))); }
        }

        public Signature Committer
        {
            get { return committer ?? (committer = new Signature(NativeMethods.git_commit_committer(Obj))); }
        }

        public List<Commit> Parents
        {
            get
            {
                if (parents == null)
                {
                    IntPtr parentCommit;
                    parents = new List<Commit>();
                    for (uint i = 0; NativeMethods.git_commit_parent(out parentCommit, Obj, i) == (int) GitErrorCodes.GIT_SUCCESS; i++)
                    {
                        parents.Add(new Commit(parentCommit));
                    }
                }
                return parents;
            }
        }
    }
}