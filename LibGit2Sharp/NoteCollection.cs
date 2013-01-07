using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A collection of <see cref = "Note"/> exposed in the <see cref = "Repository"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class NoteCollection : IEnumerable<Note>
    {
        private readonly Repository repo;
        private readonly Lazy<string> defaultNamespace;

        private const string refsNotesPrefix = "refs/notes/";

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected NoteCollection()
        { }

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
        public virtual IEnumerator<Note> GetEnumerator()
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
        public virtual string DefaultNamespace
        {
            get { return defaultNamespace.Value; }
        }

        /// <summary>
        ///   The list of canonicalized namespaces related to notes.
        /// </summary>
        public virtual IEnumerable<string> Namespaces
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
                   where refCanonical.StartsWith(refsNotesPrefix, StringComparison.Ordinal) && refCanonical != NormalizeToCanonicalName(DefaultNamespace)
                   select refCanonical);
            }
        }

        /// <summary>
        ///   Gets the collection of <see cref = "Note"/> associated with the specified <see cref = "ObjectId"/>.
        /// </summary>
        public virtual IEnumerable<Note> this[ObjectId id]
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
        public virtual IEnumerable<Note> this[string @namespace]
        {
            get
            {
                Ensure.ArgumentNotNull(@namespace, "@namespace");

                string canonicalNamespace = NormalizeToCanonicalName(@namespace);

                return Proxy.git_note_foreach(repo.Handle, canonicalNamespace,
                    (blobId,annotatedObjId) => RetrieveNote(new ObjectId(annotatedObjId), canonicalNamespace));
            }
        }

        internal Note RetrieveNote(ObjectId targetObjectId, string canonicalNamespace)
        {
            using (NoteSafeHandle noteHandle = Proxy.git_note_read(repo.Handle, canonicalNamespace, targetObjectId))
            {
                return noteHandle == null ? null :
                    Note.BuildFromPtr(noteHandle, UnCanonicalizeName(canonicalNamespace), targetObjectId);
            }
        }

        private string RetrieveDefaultNamespace()
        {
            string notesRef = Proxy.git_note_default_ref(repo.Handle);

            return UnCanonicalizeName(notesRef);
        }

        internal static string NormalizeToCanonicalName(string name)
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
        public virtual Note Add(ObjectId targetId, string message, Signature author, Signature committer, string @namespace)
        {
            Ensure.ArgumentNotNull(targetId, "targetId");
            Ensure.ArgumentNotNullOrEmptyString(message, "message");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNullOrEmptyString(@namespace, "@namespace");

            string canonicalNamespace = NormalizeToCanonicalName(@namespace);

            Remove(targetId, author, committer, @namespace);

            Proxy.git_note_create(repo.Handle, author, committer, canonicalNamespace, targetId, message, true);

            return RetrieveNote(targetId, canonicalNamespace);
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
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Note Create(ObjectId targetId, string message, Signature author, Signature committer, string @namespace)
        {
            return Add(targetId, message, author, committer, @namespace);
        }

        /// <summary>
        ///   Deletes the note on the specified object, and for the given namespace.
        /// </summary>
        /// <param name = "targetId">The target <see cref = "ObjectId"/>, for which the note will be created.</param>
        /// <param name = "author">The author.</param>
        /// <param name = "committer">The committer.</param>
        /// <param name = "namespace">The namespace on which the note will be removed. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        public virtual void Remove(ObjectId targetId, Signature author, Signature committer, string @namespace)
        {
            Ensure.ArgumentNotNull(targetId, "targetId");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNullOrEmptyString(@namespace, "@namespace");

            string canonicalNamespace = NormalizeToCanonicalName(@namespace);

            Proxy.git_note_remove(repo.Handle, canonicalNamespace, author, committer, targetId);
        }

        /// <summary>
        ///   Deletes the note on the specified object, and for the given namespace.
        /// </summary>
        /// <param name = "targetId">The target <see cref = "ObjectId"/>, for which the note will be created.</param>
        /// <param name = "author">The author.</param>
        /// <param name = "committer">The committer.</param>
        /// <param name = "namespace">The namespace on which the note will be removed. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        [Obsolete("This method will be removed in the next release. Please use Remove() instead.")]
        public virtual void Delete(ObjectId targetId, Signature author, Signature committer, string @namespace)
        {
            Remove(targetId, author, committer, @namespace);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", this.Count());
            }
        }
    }
}
