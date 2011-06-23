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
        private readonly Lazy<IEnumerable<Commit>> parents;
        private readonly Lazy<Tree> tree;

        internal Commit(ObjectId id, ObjectId treeId, Repository repo) : base(id)
        {
            this.tree = new Lazy<Tree>(() => repo.Lookup<Tree>(treeId));
            this.parents = new Lazy<IEnumerable<Commit>>(() => RetrieveParentsOfCommit(id.Oid));
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
        public Tree Tree { get { return tree.Value; } }

        /// <summary>
        ///   Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        public IEnumerable<Commit> Parents { get { return parents.Value; } }

        private IEnumerable<Commit> RetrieveParentsOfCommit(GitOid oid)    //TODO: Convert to a ParentEnumerator
        {
            IntPtr obj;
            var res = NativeMethods.git_object_lookup(out obj, repo.Handle, ref oid, GitObjectType.Commit);
            Ensure.Success(res);

            var parentsOfCommits = new List<Commit>();

            try
            {
                uint parentsCount = NativeMethods.git_commit_parentcount(obj);

                for (uint i = 0; i < parentsCount; i++)
                {
                    IntPtr parentCommit;
                    res = NativeMethods.git_commit_parent(out parentCommit, obj, i);
                    Ensure.Success(res);
                    parentsOfCommits.Add((Commit)CreateFromPtr(parentCommit, ObjectIdOf(parentCommit), repo));
                }
            }
            finally
            {
                NativeMethods.git_object_close(obj);
            }

            return parentsOfCommits;
        }

        internal static Commit BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var treeId =
                new ObjectId((GitOid) Marshal.PtrToStructure(NativeMethods.git_commit_tree_oid(obj), typeof (GitOid)));

            return new Commit(id, treeId, repo)
            {
                Message = NativeMethods.git_commit_message(obj).MarshallAsString(),
                MessageShort = NativeMethods.git_commit_message_short(obj).MarshallAsString(),
                Author = new Signature(NativeMethods.git_commit_author(obj)),
                Committer = new Signature(NativeMethods.git_commit_committer(obj)),
            };
        }
    }
}