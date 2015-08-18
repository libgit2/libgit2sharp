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
    /// A collection of <see cref="Note"/> exposed in the <see cref="Repository"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class NoteCollection : IEnumerable<Note>
    {
        internal readonly Repository repo;
        private readonly Lazy<string> defaultNamespace;

        /// <summary>
        /// Needed for mocking purposes.
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
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Note> GetEnumerator()
        {
            return this[DefaultNamespace].GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// The default namespace for notes.
        /// </summary>
        public virtual string DefaultNamespace
        {
            get { return defaultNamespace.Value; }
        }

        /// <summary>
        /// The list of canonicalized namespaces related to notes.
        /// </summary>
        public virtual IEnumerable<string> Namespaces
        {
            get { return NamespaceRefs.Select(UnCanonicalizeName); }
        }

        internal IEnumerable<string> NamespaceRefs
        {
            get
            {
                return new[] { NormalizeToCanonicalName(DefaultNamespace) }.Concat(repo.Refs
                    .Select(reference => reference.CanonicalName)
                    .Where(refCanonical => refCanonical.StartsWith(Reference.NotePrefix, StringComparison.Ordinal) && refCanonical != NormalizeToCanonicalName(DefaultNamespace)));
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="Note"/> associated with the specified <see cref="ObjectId"/>.
        /// </summary>
        public virtual IEnumerable<Note> this[ObjectId id]
        {
            get
            {
                Ensure.ArgumentNotNull(id, "id");

                return NamespaceRefs
                    .Select(ns => this[ns, id])
                    .Where(n => n != null);
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="Note"/> associated with the specified namespace.
        /// <para>This is similar to the 'get notes list' command.</para>
        /// </summary>
        public virtual IEnumerable<Note> this[string @namespace]
        {
            get
            {
                Ensure.ArgumentNotNull(@namespace, "@namespace");

                string canonicalNamespace = NormalizeToCanonicalName(@namespace);

                return Proxy.git_note_foreach(repo.Handle,
                                              canonicalNamespace,
                                              (blobId, annotatedObjId) => this[canonicalNamespace, annotatedObjId]);
            }
        }

        /// <summary>
        /// Gets the <see cref="Note"/> associated with the specified objectId and the specified namespace.
        /// </summary>
        public virtual Note this[string @namespace, ObjectId id]
        {
            get
            {
                Ensure.ArgumentNotNull(id, "id");
                Ensure.ArgumentNotNull(@namespace, "@namespace");

                string canonicalNamespace = NormalizeToCanonicalName(@namespace);

                using (NoteSafeHandle noteHandle = Proxy.git_note_read(repo.Handle, canonicalNamespace, id))
                {
                    return noteHandle == null
                        ? null
                        : Note.BuildFromPtr(noteHandle, UnCanonicalizeName(canonicalNamespace), id);
                }
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

            if (name.LooksLikeNote())
            {
                return name;
            }

            return string.Concat(Reference.NotePrefix, name);
        }

        internal static string UnCanonicalizeName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (!name.LooksLikeNote())
            {
                return name;
            }

            return name.Substring(Reference.NotePrefix.Length);
        }

        /// <summary>
        /// Creates or updates a <see cref="Note"/> on the specified object, and for the given namespace.
        /// <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="targetId">The target <see cref="ObjectId"/>, for which the note will be created.</param>
        /// <param name="message">The note message.</param>
        /// <param name="namespace">The namespace on which the note will be created. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        /// <returns>The note which was just saved.</returns>
        [Obsolete("This method will be removed in the next release. Please use Add(ObjectId, string, Signature, Signature, string) instead.")]
        public virtual Note Add(ObjectId targetId, string message, string @namespace)
        {
            Signature author = repo.Config.BuildSignatureOrThrow(DateTimeOffset.Now);

            return Add(targetId, message, author, author, @namespace);
        }

        /// <summary>
        /// Creates or updates a <see cref="Note"/> on the specified object, and for the given namespace.
        /// </summary>
        /// <param name="targetId">The target <see cref="ObjectId"/>, for which the note will be created.</param>
        /// <param name="message">The note message.</param>
        /// <param name="author">The author.</param>
        /// <param name="committer">The committer.</param>
        /// <param name="namespace">The namespace on which the note will be created. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
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

            Proxy.git_note_create(repo.Handle, canonicalNamespace, author, committer, targetId, message, true);

            return this[canonicalNamespace, targetId];
        }

        /// <summary>
        /// Deletes the note on the specified object, and for the given namespace.
        /// <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="targetId">The target <see cref="ObjectId"/>, for which the note will be created.</param>
        /// <param name="namespace">The namespace on which the note will be removed. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        [Obsolete("This method will be removed in the next release. Please use Remove(ObjectId, Signature, Signature, string) instead.")]
        public virtual void Remove(ObjectId targetId, string @namespace)
        {
            Signature author = repo.Config.BuildSignatureOrThrow(DateTimeOffset.Now);

            Remove(targetId, author, author, @namespace);
        }

        /// <summary>
        /// Deletes the note on the specified object, and for the given namespace.
        /// </summary>
        /// <param name="targetId">The target <see cref="ObjectId"/>, for which the note will be created.</param>
        /// <param name="author">The author.</param>
        /// <param name="committer">The committer.</param>
        /// <param name="namespace">The namespace on which the note will be removed. It can be either a canonical namespace or an abbreviated namespace ('refs/notes/myNamespace' or just 'myNamespace').</param>
        public virtual void Remove(ObjectId targetId, Signature author, Signature committer, string @namespace)
        {
            Ensure.ArgumentNotNull(targetId, "targetId");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNullOrEmptyString(@namespace, "@namespace");

            string canonicalNamespace = NormalizeToCanonicalName(@namespace);

            Proxy.git_note_remove(repo.Handle, canonicalNamespace, author, committer, targetId);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count());
            }
        }
    }
}
