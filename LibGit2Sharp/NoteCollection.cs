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
            throw new NotImplementedException();
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

        internal Note RetrieveNote(ObjectId id, string canonicalNamespace)
        {
            using (NoteSafeHandle noteHandle = BuildNoteSafeHandle(id, canonicalNamespace))
            {
                if (noteHandle == null)
                {
                    return null;
                }

                return Note.BuildFromPtr(repo, UnCanonicalizeName(canonicalNamespace), id, noteHandle);
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

            if (res == (int)GitErrorCode.GIT_ENOTFOUND)
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
    }
}
