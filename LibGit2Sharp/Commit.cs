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
        private readonly Repository repo;

        private readonly GitObjectLazyGroup group;
        private readonly ILazy<Tree> lazyTree;
        private readonly ILazy<Signature> lazyAuthor;
        private readonly ILazy<Signature> lazyCommitter;
        private readonly ILazy<string> lazyMessage;
        private readonly ILazy<string> lazyEncoding;

        private readonly Lazy<IEnumerable<Commit>> lazyParents;
        private readonly Lazy<string> lazyShortMessage;
        private readonly Lazy<IEnumerable<Note>> lazyNotes;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Commit()
        { }

        internal Commit(Repository repo, ObjectId id)
            : base(id)
        {
            this.repo = repo;

            lazyTree = GitObjectLazyGroup.Singleton(this.repo, id, obj => new Tree(this.repo, Proxy.git_commit_tree_oid(obj), null));

            group = new GitObjectLazyGroup(this.repo, id);
            lazyAuthor = group.AddLazy(Proxy.git_commit_author);
            lazyCommitter = group.AddLazy(Proxy.git_commit_committer);
            lazyMessage = group.AddLazy(Proxy.git_commit_message);
            lazyEncoding = group.AddLazy(RetrieveEncodingOf);

            lazyParents = new Lazy<IEnumerable<Commit>>(() => RetrieveParentsOfCommit(id));
            lazyShortMessage = new Lazy<string>(ExtractShortMessage);
            lazyNotes = new Lazy<IEnumerable<Note>>(() => RetrieveNotesOfCommit(id).ToList());
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
        public virtual IEnumerable<Commit> Parents { get { return lazyParents.Value; } }

        /// <summary>
        ///   Gets The count of parent commits.
        /// </summary>
        public virtual int ParentsCount
        {
            get
            {
                return Proxy.git_commit_parentcount(repo.Handle, Id);
            }
        }

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

        private IEnumerable<Commit> RetrieveParentsOfCommit(ObjectId oid)
        {
            using (var obj = new ObjectSafeWrapper(oid, repo.Handle))
            {
                int parentsCount = Proxy.git_commit_parentcount(obj);

                for (uint i = 0; i < parentsCount; i++)
                {
                    ObjectId parentCommitId = Proxy.git_commit_parent_oid(obj.ObjectPtr, i);
                    yield return new Commit(repo, parentCommitId);
                }
            }
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
    }
}
