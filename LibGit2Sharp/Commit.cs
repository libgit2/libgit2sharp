using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A Commit
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Commit : GitObject
    {
        private readonly GitObjectLazyGroup group1;
        private readonly GitObjectLazyGroup group2;
        private readonly ILazy<Tree> lazyTree;
        private readonly ILazy<Signature> lazyAuthor;
        private readonly ILazy<Signature> lazyCommitter;
        private readonly ILazy<string> lazyMessage;
        private readonly ILazy<string> lazyMessageShort;
        private readonly ILazy<string> lazyEncoding;

        private readonly ParentsCollection parents;
        private readonly Lazy<IEnumerable<Note>> lazyNotes;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Commit()
        { }

        internal Commit(Repository repo, ObjectId id)
            : base(repo, id)
        {
            lazyTree = GitObjectLazyGroup.Singleton(this.repo, id, obj => new Tree(this.repo, Proxy.git_commit_tree_id(obj), null));

            group1 = new GitObjectLazyGroup(this.repo, id);
            lazyAuthor = group1.AddLazy(Proxy.git_commit_author);
            lazyCommitter = group1.AddLazy(Proxy.git_commit_committer);
            group2 = new GitObjectLazyGroup(this.repo, id);
            lazyMessage = group2.AddLazy(Proxy.git_commit_message);
            lazyMessageShort = group2.AddLazy(Proxy.git_commit_summary);
            lazyEncoding = group2.AddLazy(RetrieveEncodingOf);

            lazyNotes = new Lazy<IEnumerable<Note>>(() => RetrieveNotesOfCommit(id).ToList());

            parents = new ParentsCollection(repo, id);
        }

        /// <summary>
        /// Gets the <see cref="TreeEntry"/> pointed at by the <paramref name="relativePath"/> in the <see cref="Tree"/>.
        /// </summary>
        /// <param name="relativePath">Path to the <see cref="TreeEntry"/> from the tree in this <see cref="Commit"/></param>
        /// <returns><c>null</c> if nothing has been found, the <see cref="TreeEntry"/> otherwise.</returns>
        public virtual TreeEntry this[string relativePath]
        {
            get { return Tree[relativePath]; }
        }

        /// <summary>
        /// Gets the commit message.
        /// </summary>
        public virtual string Message { get { return lazyMessage.Value; } }

        /// <summary>
        /// Gets the short commit message which is usually the first line of the commit.
        /// </summary>
        public virtual string MessageShort { get { return lazyMessageShort.Value; } }

        /// <summary>
        /// Gets the encoding of the message.
        /// </summary>
        public virtual string Encoding { get { return lazyEncoding.Value; } }

        /// <summary>
        /// Gets the author of this commit.
        /// </summary>
        public virtual Signature Author { get { return lazyAuthor.Value; } }

        /// <summary>
        /// Gets the committer.
        /// </summary>
        public virtual Signature Committer { get { return lazyCommitter.Value; } }

        /// <summary>
        /// Gets the Tree associated to this commit.
        /// </summary>
        public virtual Tree Tree { get { return lazyTree.Value; } }

        /// <summary>
        /// Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        public virtual IEnumerable<Commit> Parents { get { return parents; } }

        /// <summary>
        /// Gets the notes of this commit.
        /// </summary>
        public virtual IEnumerable<Note> Notes { get { return lazyNotes.Value; } }

        private IEnumerable<Note> RetrieveNotesOfCommit(ObjectId oid)
        {
            return repo.Notes[oid];
        }

        private static string RetrieveEncodingOf(ObjectHandle obj)
        {
            string encoding = Proxy.git_commit_message_encoding(obj);

            return encoding ?? "UTF-8";
        }

        /// <summary>
        /// Prettify a commit message
        /// <para>
        /// Remove comment lines and trailing lines
        /// </para>
        /// </summary>
        /// <returns>The prettified message</returns>
        /// <param name="message">The message to prettify.</param>
        /// <param name="commentChar">Comment character. Lines starting with it will be removed</param>
        public static string PrettifyMessage(string message, char commentChar)
        {
            return Proxy.git_message_prettify(message, commentChar);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0} {1}",
                                     Id.ToString(7),
                                     MessageShort);
            }
        }

        /// <summary>
        /// Extract the signature data from this commit
        /// </summary>
        /// <returns>The signature and the signed data</returns>
        /// <param name="repo">The repository in which the object lives</param>
        /// <param name="id">The commit to extract the signature from</param>
        /// <param name="field">The header field which contains the signature; use null for the default of "gpgsig"</param>
        public static SignatureInfo ExtractSignature(Repository repo, ObjectId id, string field)
        {
            return Proxy.git_commit_extract_signature(repo.Handle, id, field);
        }

        /// <summary>
        /// Extract the signature data from this commit
        /// <para>
        /// The overload uses the default header field "gpgsig"
        /// </para>
        /// </summary>
        /// <returns>The signature and the signed data</returns>
        /// <param name="repo">The repository in which the object lives</param>
        /// <param name="id">The commit to extract the signature from</param>
        public static SignatureInfo ExtractSignature(Repository repo, ObjectId id)
        {
            return Proxy.git_commit_extract_signature(repo.Handle, id, null);
        }

        /// <summary>
        /// Create a commit in-memory
        /// <para>
        /// Prettifing the message includes:
        /// * Removing empty lines from the beginning and end.
        /// * Removing trailing spaces from every line.
        /// * Turning multiple consecutive empty lines between paragraphs into just one empty line.
        /// * Ensuring the commit message ends with a newline.
        /// * Removing every line starting with the <paramref name="commentChar"/>.
        /// </para>
        /// </summary>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="tree">The <see cref="Tree"/> of the <see cref="Commit"/> to be created.</param>
        /// <param name="parents">The parents of the <see cref="Commit"/> to be created.</param>
        /// <param name="prettifyMessage">True to prettify the message, or false to leave it as is.</param>
        /// <param name="commentChar">When non null, lines starting with this character will be stripped if prettifyMessage is true.</param>
        /// <returns>The contents of the commit object.</returns>
        public static string CreateBuffer(Signature author, Signature committer, string message, Tree tree, IEnumerable<Commit> parents, bool prettifyMessage, char? commentChar)
        {
            Ensure.ArgumentNotNull(message, "message");
            Ensure.ArgumentDoesNotContainZeroByte(message, "message");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNull(tree, "tree");
            Ensure.ArgumentNotNull(parents, "parents");

            if (prettifyMessage)
            {
                message = Proxy.git_message_prettify(message, commentChar);
            }

            return Proxy.git_commit_create_buffer(tree.repo.Handle, author, committer, message, tree, parents.ToArray());
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
                using (var obj = new ObjectSafeWrapper(commitId, repo.Handle))
                {
                    int parentsCount = _count.Value;
                    var parents = new List<Commit>(parentsCount);

                    for (uint i = 0; i < parentsCount; i++)
                    {
                        ObjectId parentCommitId = Proxy.git_commit_parent_id(obj.ObjectPtr, i);
                        parents.Add(new Commit(repo, parentCommitId));
                    }

                    return parents;
                }
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
