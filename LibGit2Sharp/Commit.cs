using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Commit
    /// </summary>
    public class Commit : GitObject
    {
        private readonly GitObjectLazyGroup group;
        private readonly ILazy<Tree> lazyTree;
        private readonly ILazy<Signature> lazyAuthor;
        private readonly ILazy<Signature> lazyCommitter;
        private readonly ILazy<string> lazyMessage;
        private readonly ILazy<string> lazyEncoding;

        private readonly ParentsCollection parents;
        private readonly Lazy<string> lazyShortMessage;
        private readonly Lazy<IEnumerable<Note>> lazyNotes;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Commit()
        { }

        internal Commit(Repository repo, ObjectId id)
            : base(repo, id)
        {
            lazyTree = GitObjectLazyGroup.Singleton(this.repo, id, obj => new Tree(this.repo, Proxy.git_commit_tree_oid(obj), null));

            group = new GitObjectLazyGroup(this.repo, id);
            lazyAuthor = group.AddLazy(Proxy.git_commit_author);
            lazyCommitter = group.AddLazy(Proxy.git_commit_committer);
            lazyMessage = group.AddLazy(Proxy.git_commit_message);
            lazyEncoding = group.AddLazy(RetrieveEncodingOf);

            lazyShortMessage = new Lazy<string>(ExtractShortMessage);
            lazyNotes = new Lazy<IEnumerable<Note>>(() => RetrieveNotesOfCommit(id).ToList());

            parents = new ParentsCollection(repo, id);
        }

        /// <summary>
        ///   Gets the <see cref = "TreeEntry" /> pointed at by the <paramref name = "relativePath" /> in the <see cref = "Tree" />.
        /// </summary>
        /// <param name = "relativePath">The relative path to the <see cref = "TreeEntry" /> from the <see cref = "Commit" /> working directory.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref = "TreeEntry" /> otherwise.</returns>
        public virtual TreeEntry this[string relativePath]
        {
            get { return Tree[relativePath]; }
        }

        /// <summary>
        ///   Gets the commit message.
        /// </summary>
        public virtual string Message { get { return lazyMessage.Value; } }

        /// <summary>
        ///   Gets the short commit message which is usually the first line of the commit.
        /// </summary>
        public virtual string MessageShort { get { return lazyShortMessage.Value; } }

        /// <summary>
        ///   Gets the encoding of the message.
        /// </summary>
        public virtual string Encoding { get { return lazyEncoding.Value; } }

        /// <summary>
        ///   Gets the author of this commit.
        /// </summary>
        public virtual Signature Author { get { return lazyAuthor.Value; } }

        /// <summary>
        ///   Gets the committer.
        /// </summary>
        public virtual Signature Committer { get { return lazyCommitter.Value; } }

        /// <summary>
        ///   Gets the Tree associated to this commit.
        /// </summary>
        public virtual Tree Tree { get { return lazyTree.Value; } }

        /// <summary>
        ///   Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        public virtual IEnumerable<Commit> Parents { get { return parents; } }

        /// <summary>
        ///   Gets The count of parent commits.
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use Parents.Count() instead.")]
        public virtual int ParentsCount { get { return Parents.Count(); } }

        /// <summary>
        ///   Gets the notes of this commit.
        /// </summary>
        public virtual IEnumerable<Note> Notes { get { return lazyNotes.Value; } }

        private string ExtractShortMessage()
        {
            if (Message == null)
            {
                return string.Empty; //TODO: Add some test coverage
            }

            return Message.Split('\n')[0];
        }

        private IEnumerable<Note> RetrieveNotesOfCommit(ObjectId oid)
        {
            return repo.Notes[oid];
        }

        private static string RetrieveEncodingOf(GitObjectSafeHandle obj)
        {
            string encoding = Proxy.git_commit_message_encoding(obj);

            return encoding ?? "UTF-8";
        }

        private class ParentsCollection : ICollection<Commit>
        {
            private readonly Lazy<ICollection<Commit>> _parents;
            private readonly Lazy<int> _count;

            public ParentsCollection(Repository repo, ObjectId commitId)
            {
                _count = new Lazy<int>(() => Proxy.git_commit_parentcount(repo.Handle, commitId));
                _parents = new Lazy<ICollection<Commit>>(() => RetrieveParentsOfCommit(repo, commitId));
            }

            private ICollection<Commit> RetrieveParentsOfCommit(Repository repo, ObjectId commitId)
            {
                var parents = new List<Commit>();

                using (var obj = new ObjectSafeWrapper(commitId, repo.Handle))
                {
                    int parentsCount = _count.Value;

                    for (uint i = 0; i < parentsCount; i++)
                    {
                        ObjectId parentCommitId = Proxy.git_commit_parent_oid(obj.ObjectPtr, i);
                        parents.Add(new Commit(repo, parentCommitId));
                    }
                }

                return parents;
            }

            public IEnumerator<Commit> GetEnumerator()
            {
                return _parents.Value.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(Commit item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(Commit item)
            {
                return _parents.Value.Contains(item);
            }

            public void CopyTo(Commit[] array, int arrayIndex)
            {
                _parents.Value.CopyTo(array, arrayIndex);
            }

            public bool Remove(Commit item)
            {
                throw new NotSupportedException();
            }

            public int Count
            {
                get { return _count.Value; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }
        }
    }
}
