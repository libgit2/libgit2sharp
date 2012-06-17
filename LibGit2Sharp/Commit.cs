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
        private readonly Lazy<IEnumerable<Commit>> parents;
        private readonly Lazy<Tree> tree;
        private readonly Lazy<string> shortMessage;
        private readonly Lazy<IEnumerable<Note>> notes;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Commit()
        { }

        internal Commit(ObjectId id, ObjectId treeId, Repository repo)
            : base(id)
        {
            tree = new Lazy<Tree>(() => repo.Lookup<Tree>(treeId));
            parents = new Lazy<IEnumerable<Commit>>(() => RetrieveParentsOfCommit(id));
            shortMessage = new Lazy<string>(ExtractShortMessage);
            notes = new Lazy<IEnumerable<Note>>(() => RetrieveNotesOfCommit(id).ToList());
            this.repo = repo;
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
        public virtual string Message { get; private set; }

        /// <summary>
        ///   Gets the short commit message which is usually the first line of the commit.
        /// </summary>
        public virtual string MessageShort
        {
            get { return shortMessage.Value; }
        }

        private string ExtractShortMessage()
        {
            if (Message == null)
            {
                return string.Empty; //TODO: Add some test coverage
            }

            return Message.Split('\n')[0];
        }

        /// <summary>
        ///   Gets the encoding of the message.
        /// </summary>
        public virtual string Encoding { get; private set; }

        /// <summary>
        ///   Gets the author of this commit.
        /// </summary>
        public virtual Signature Author { get; private set; }

        /// <summary>
        ///   Gets the committer.
        /// </summary>
        public virtual Signature Committer { get; private set; }

        /// <summary>
        ///   Gets the Tree associated to this commit.
        /// </summary>
        public virtual Tree Tree
        {
            get { return tree.Value; }
        }

        /// <summary>
        ///   Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        public virtual IEnumerable<Commit> Parents
        {
            get { return parents.Value; }
        }

        /// <summary>
        ///   Gets The count of parent commits.
        /// </summary>
        public int ParentsCount
        {
            get
            {
                using (var obj = new ObjectSafeWrapper(Id, repo))
                {
                    return (int)NativeMethods.git_commit_parentcount(obj.ObjectPtr);
                }
            }
        }

        /// <summary>
        ///   Gets the notes of this commit.
        /// </summary>
        public IEnumerable<Note> Notes
        {
            get { return notes.Value; }
        }

        private IEnumerable<Commit> RetrieveParentsOfCommit(ObjectId oid)
        {
            using (var obj = new ObjectSafeWrapper(oid, repo))
            {
                uint parentsCount = NativeMethods.git_commit_parentcount(obj.ObjectPtr);

                for (uint i = 0; i < parentsCount; i++)
                {
                    using (var parentCommit = GetParentCommitHandle(i, obj))
                    {
                        yield return BuildFromPtr(parentCommit, ObjectIdOf(parentCommit), repo);
                    }
                }
            }
        }

        private IEnumerable<Note> RetrieveNotesOfCommit(ObjectId oid)
        {
            return repo.Notes[oid];
        }

        internal static Commit BuildFromPtr(GitObjectSafeHandle obj, ObjectId id, Repository repo)
        {
            ObjectId treeId = NativeMethods.git_commit_tree_oid(obj).MarshalAsObjectId();

            return new Commit(id, treeId, repo)
                       {
                           Message = NativeMethods.git_commit_message(obj),
                           Encoding = RetrieveEncodingOf(obj),
                           Author = new Signature(NativeMethods.git_commit_author(obj)),
                           Committer = new Signature(NativeMethods.git_commit_committer(obj)),
                       };
        }

        private static GitObjectSafeHandle GetParentCommitHandle(uint i, ObjectSafeWrapper obj)
        {
            GitObjectSafeHandle parentCommit;
            Ensure.Success(NativeMethods.git_commit_parent(out parentCommit, obj.ObjectPtr, i));
            return parentCommit;
        }

        private static string RetrieveEncodingOf(GitObjectSafeHandle obj)
        {
            string encoding = NativeMethods.git_commit_message_encoding(obj);

            return encoding ?? "UTF-8";
        }
    }
}
