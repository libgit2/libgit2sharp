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
                    for (uint i = 0; NativeMethods.git_commit_parent(out parentCommit, Obj, i) == (int) GitErrorCode.GIT_SUCCESS; i++)
                    {
                        parents.Add(new Commit(parentCommit));
                    }
                }
                return parents;
            }
        }
    }
}