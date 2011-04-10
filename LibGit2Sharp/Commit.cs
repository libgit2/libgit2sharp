using System;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Commit
    /// </summary>
    public class Commit : GitObject
    {
        //public Commit(Repository repo, Signature author, Signature committer, string message = null, Tree tree = null, IEnumerable<Commit> parents = null, string updateRefName = null)
        //{
        //}

        private readonly Repository repo;
        private List<Commit> parents;

        internal Commit(IntPtr obj, Repository repo, ObjectId id = null)
            : base(obj, id)
        {
            this.repo = repo;
            Message = NativeMethods.git_commit_message(obj);
            MessageShort = NativeMethods.git_commit_message_short(obj);
            Author = new Signature(NativeMethods.git_commit_author(obj));
            Committer = new Signature(NativeMethods.git_commit_committer(obj));
        }

        /// <summary>
        ///   Gets the commit message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///   Gets the short commit message which is usually the first line of the commit.
        /// </summary>
        public string MessageShort { get; private set; }

        /// <summary>
        ///   Gets the author of this commit.
        /// </summary>
        public Signature Author { get; private set; }

        /// <summary>
        ///   Gets the committer.
        /// </summary>
        public Signature Committer { get; private set; }

        /// <summary>
        ///   Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        public List<Commit> Parents
        {
            get
            {
                if (parents == null)
                {
                    var oid = Id.Oid;
                    IntPtr obj;
                    var res = NativeMethods.git_object_lookup(out obj, repo.Handle, ref oid, GitObjectType.Commit);
                    Ensure.Success(res);

                    try
                    {
                        parents = new List<Commit>();

                        uint parentsCount = NativeMethods.git_commit_parentcount(obj);

                        for (uint i = 0; i < parentsCount; i++)
                        {
                            IntPtr parentCommit;
                            res = NativeMethods.git_commit_parent(out parentCommit, obj, i);
                            Ensure.Success(res);
                            parents.Add(new Commit(parentCommit, repo));
                            NativeMethods.git_object_close(parentCommit);
                        }
                    }
                    finally
                    {
                        NativeMethods.git_object_close(obj);
                    }
                }
                return parents;
            }
        }
    }
}