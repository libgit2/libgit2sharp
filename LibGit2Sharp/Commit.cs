﻿using System;
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

        internal Commit(IntPtr obj, ObjectId id, Repository repo) : base(obj, id)
        {
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

        public ObjectId TreeSha { get; private set; }

        public Tree Tree { get { return repo.Lookup<Tree>(TreeSha); } }

        /// <summary>
        ///   Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        public IEnumerable<Commit> Parents
        {
            get { return parents ?? (parents = RetrieveParentsOfCommit()); }
        }

        private List<Commit> RetrieveParentsOfCommit()
        {

            parents = new List<Commit>();

            uint parentsCount = NativeMethods.git_commit_parentcount(Obj);

            for (uint i = 0; i < parentsCount; i++)
            {
                IntPtr parentCommit;
                var res = NativeMethods.git_commit_parent(out parentCommit, Obj, i);
                Ensure.Success(res);
                parents.Add((Commit) CreateFromPtr(parentCommit, RetrieveObjectIfOf(parentCommit), repo));
            }

            return parents;
        }

        internal static Commit BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            return new Commit(obj, id, repo)
            {
                Message = NativeMethods.git_commit_message(obj),
                MessageShort = NativeMethods.git_commit_message_short(obj),
                Author = new Signature(NativeMethods.git_commit_author(obj)),
                Committer = new Signature(NativeMethods.git_commit_committer(obj)),
                TreeSha =  new ObjectId((GitOid) Marshal.PtrToStructure(NativeMethods.git_commit_tree_oid(obj), typeof (GitOid)))
            };
        }

    }
}