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
    ///   A collection of <see cref = "Note"/> exposed in the <see cref = "Repository"/>.
    /// </summary>
    public class NoteCollection : IEnumerable<Note>
    {
        private readonly Repository repo;
        private readonly Lazy<string> defaultNamespace;

        private const string refsNotesPrefix = "refs/notes/";

        internal NoteCollection(Repository repo)
        {
            this.repo = repo;
            defaultNamespace = new Lazy<string>(RetrieveDefaultNamespace);
        }

        #region Implementation of IEnumerable

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Note> GetEnumerator()
        {
            return this[DefaultNamespace].GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   The default namespace for notes.
        /// </summary>
        public string DefaultNamespace
        {
            get { return defaultNamespace.Value; }
        }

        /// <summary>
        ///   The list of canonicalized namespaces related to notes.
        /// </summary>
        public IEnumerable<string> Namespaces
        {
            get
            {
                return NamespaceRefs.Select(UnCanonicalizeName);
            }
        }

        internal IEnumerable<string> NamespaceRefs
        {
            get
            {
                return new[] { NormalizeToCanonicalName(DefaultNamespace) }.Concat(
                   from reference in repo.Refs
                   select reference.CanonicalName into refCanonical
                   where refCanonical.StartsWith(refsNotesPrefix) && refCanonical != NormalizeToCanonicalName(DefaultNamespace)
                   select refCanonical);
            }
        }

        /// <summary>
        ///   Gets the collection of <see cref = "Note"/> associated with the specified <see cref = "ObjectId"/>.
        /// </summary>
        public IEnumerable<Note> this[ObjectId id]
        {
            get
            {
                Ensure.ArgumentNotNull(id, "id");

                return NamespaceRefs
                    .Select(ns => RetrieveNote(id, ns))
                    .Where(n => n != null);
            }
        }

        /// <summary>
        ///   Gets the collection of <see cref = "Note"/> associated with the specified namespace.
        ///   <para>This is similar to the 'get notes list' command.</para>
        /// </summary>
        public IEnumerable<Note> this[string @namespace]
        {
            get
            {
                Ensure.ArgumentNotNull(@namespace, "@namespace");

                string canonicalNamespace = NormalizeToCanonicalName(@namespace);
                var notesOidRetriever = new NotesOidRetriever(repo, canonicalNamespace);

                return notesOidRetriever.Retrieve().Select(oid => RetrieveNote(new ObjectId(oid), canonicalNamespace));
            }
        }

        internal Note RetrieveNote(ObjectId targetObjectId, string canonicalNamespace)
        {
            using (NoteSafeHandle noteHandle = BuildNoteSafeHandle(targetObjectId, canonicalNamespace))
            {
                if (noteHandle == null)
                {
                    return null;
                }

                return Note.BuildFromPtr(repo, UnCanonicalizeName(canonicalNamespace), targetObjectId, noteHandle);
            }
        }

        private string RetrieveDefaultNamespace()
        {
            string notesRef;
            Ensure.Success(NativeMethods.git_note_default_ref(out notesRef, repo.Handle));

            return UnCanonicalizeName(notesRef);
        }

        private NoteSafeHandle BuildNoteSafeHandle(ObjectId id, string canonicalNamespace)
        {
            NoteSafeHandle noteHandle;
            GitOid oid = id.Oid;

            int res = NativeMethods.git_note_read(out noteHandle, repo.Handle, canonicalNamespace, ref oid);

            if (res == (int)GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.Success(res);

            return noteHandle;
        }

        internal string NormalizeToCanonicalName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (name.StartsWith(refsNotesPrefix, StringComparison.Ordinal))
            {
                return name;
            }

            return string.Concat(refsNotesPrefix, name);
        }

        internal string UnCanonicalizeName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (!name.StartsWith(refsNotesPrefix, StringComparison.Ordinal))
            {
                return name;
            }

            return name.Substring(refsNotesPrefix.Length);
        }

        /// <summary>
        ///   Creates or updates a <see cref = "Note"/> on the specified object, and for the given namespace.
        /// </summary>
        /// <param name = "targetId">The target <see cref = "ObjectId"/>, for which the note will be created.</param>
        /// <param name = "message">The note message.</param>
        /// <param name = "author">The author.</param>
        /// <param name = "committer">The committer.</param>
        /// <param name = "namespace">The namespace on which the note will be created. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        /// <returns>The note which was just saved.</returns>
        public Note Add(ObjectId targetId, string message, Signature author, Signature committer, string @namespace)
        {
            Ensure.ArgumentNotNull(targetId, "targetId");
            Ensure.ArgumentNotNullOrEmptyString(message, "message");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNullOrEmptyString(@namespace, "@namespace");

            string canonicalNamespace = NormalizeToCanonicalName(@namespace);

            GitOid oid = targetId.Oid;

            Remove(targetId, author, committer, @namespace);

            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            {
                GitOid noteOid;
                Ensure.Success(NativeMethods.git_note_create(out noteOid, repo.Handle, authorHandle, committerHandle, canonicalNamespace, ref oid, message));
            }

            return RetrieveNote(targetId, canonicalNamespace);
        }

        /// <summary>
        ///   Deletes the note on the specified object, and for the given namespace.
        /// </summary>
        /// <param name = "targetId">The target <see cref = "ObjectId"/>, for which the note will be created.</param>
        /// <param name = "author">The author.</param>
        /// <param name = "committer">The committer.</param>
        /// <param name = "namespace">The namespace on which the note will be removed. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        public void Remove(ObjectId targetId, Signature author, Signature committer, string @namespace)
        {
            Ensure.ArgumentNotNull(targetId, "targetId");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNullOrEmptyString(@namespace, "@namespace");

            string canonicalNamespace = NormalizeToCanonicalName(@namespace);

            GitOid oid = targetId.Oid;
            int res;

            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            {
                res = NativeMethods.git_note_remove(repo.Handle, canonicalNamespace, authorHandle, committerHandle, ref oid);
            }

            if (res == (int)GitErrorCode.NotFound)
            {
                return;
            }

            Ensure.Success(res);
        }

        private class NotesOidRetriever
        {
            private readonly List<GitOid> notesOid = new List<GitOid>();

            internal NotesOidRetriever(Repository repo, string canonicalNamespace)
            {
                Ensure.Success(NativeMethods.git_note_foreach(repo.Handle, canonicalNamespace, NoteListCallBack, IntPtr.Zero));
            }

            private int NoteListCallBack(GitNoteData noteData, IntPtr intPtr)
            {
                notesOid.Add(noteData.TargetOid);

                return 0;
            }

            public IEnumerable<GitOid> Retrieve()
            {
                return notesOid;
            }
        }
    }
}
