using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Commit
    /// </summary>
    public class Commit : GitObject
    {
        private readonly Repository repo;
        private List<Commit> parents;
        private Tree tree;
        private readonly ObjectId treeId;

        internal Commit(ObjectId id, ObjectId treeId, Repository repo) : base(id)
        {
            this.treeId = treeId;
            this.repo = repo;
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
        ///   Gets the Tree associated to this commit.
        /// </summary>
        public Tree Tree { get { return tree ?? (tree = repo.Lookup<Tree>(treeId)); } }

        /// <summary>
        ///   Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        public IEnumerable<Commit> Parents
        {
            get { return parents ?? (parents = RetrieveParentsOfCommit(Id.Oid)); }
        }

        private List<Commit> RetrieveParentsOfCommit(GitOid oid)
        {
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
                    parents.Add((Commit)CreateFromPtr(parentCommit, ObjectIdOf(parentCommit), repo));
                }
            }
            finally
            {
                NativeMethods.git_object_close(obj);
            }

            return parents;
        }

        internal static Commit BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var treeId =
                new ObjectId((GitOid) Marshal.PtrToStructure(NativeMethods.git_commit_tree_oid(obj), typeof (GitOid)));

            return new Commit(id, treeId, repo)
            {
                Message = NativeMethods.git_commit_message(obj),
                MessageShort = NativeMethods.git_commit_message_short(obj),
                Author = new Signature(NativeMethods.git_commit_author(obj)),
                Committer = new Signature(NativeMethods.git_commit_committer(obj)),
            };
        }
    }
}