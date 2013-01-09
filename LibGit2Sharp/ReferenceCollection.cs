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
    ///   The Collection of references in a <see cref = "Repository" />
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ReferenceCollection : IEnumerable<Reference>
    {
        internal readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected ReferenceCollection()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ReferenceCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal ReferenceCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Reference" /> with the specified name.
        /// </summary>
        /// <param name = "name">The canonical name of the reference to resolve.</param>
        /// <returns>The resolved <see cref = "LibGit2Sharp.Reference" /> if it has been found, null otherwise.</returns>
        public virtual Reference this[string name]
        {
            get { return Resolve<Reference>(name); }
        }

        #region IEnumerable<Reference> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Reference> GetEnumerator()
        {
            return Proxy.git_reference_list(repo.Handle, GitReferenceType.ListAll)
                .Select(n => this[n])
                .GetEnumerator();
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
        ///   Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name = "name">The canonical name of the reference to create.</param>
        /// <param name = "targetId">Id of the target object.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetId, "targetId");

            using (ReferenceSafeHandle handle = Proxy.git_reference_create_oid(repo.Handle, name, targetId, allowOverwrite))
            {
                return (DirectReference)Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        ///   Creates a symbolic reference  with the specified name and target
        /// </summary>
        /// <param name = "name">The canonical name of the reference to create.</param>
        /// <param name = "targetRef">The target reference.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            using (ReferenceSafeHandle handle = Proxy.git_reference_create_symbolic(repo.Handle, name, targetRef.CanonicalName, allowOverwrite))
            {
                return (SymbolicReference)Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        ///   Creates a direct or symbolic reference with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "target">The target which can be either a sha or the canonical name of another reference.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Reference Create(string name, string target, bool allowOverwrite = false)
        {
            return this.Add(name, target, allowOverwrite);
        }

        /// <summary>
        ///   Remove a reference from the repository
        /// </summary>
        /// <param name = "reference">The reference to delete.</param>
        public virtual void Remove(Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

            using (ReferenceSafeHandle handle = RetrieveReferencePtr(reference.CanonicalName))
            {
                Proxy.git_reference_delete(handle);
            }
        }

        /// <summary>
        ///   Delete a reference with the specified name
        /// </summary>
        /// <param name = "name">The name of the reference to delete.</param>
        [Obsolete("This method will be removed in the next release. Please use Remove() instead.")]
        public virtual void Delete(string name)
        {
            this.Remove(name);
        }

        /// <summary>
        ///   Rename an existing reference with a new name
        /// </summary>
        /// <param name = "reference">The reference to rename.</param>
        /// <param name = "newName">The new canonical name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual Reference Move(Reference reference, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNull(reference, "reference");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            using (ReferenceSafeHandle handle = RetrieveReferencePtr(reference.CanonicalName))
            {
                Proxy.git_reference_rename(handle, newName, allowOverwrite);

                return Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        internal T Resolve<T>(string name) where T : Reference
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(name, false))
            {
                return referencePtr == null ? null : Reference.BuildFromPtr<T>(referencePtr, repo);
            }
        }

        /// <summary>
        ///   Updates the target of a direct reference.
        /// </summary>
        /// <param name = "directRef">The direct reference which target should be updated.</param>
        /// <param name = "targetId">The new target.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId)
        {
            Ensure.ArgumentNotNull(directRef, "directRef");
            Ensure.ArgumentNotNull(targetId, "targetId");

            return UpdateTarget(directRef, targetId,
                (h, id) => Proxy.git_reference_set_oid(h, id));
        }

        /// <summary>
        ///   Updates the target of a symbolic reference.
        /// </summary>
        /// <param name = "symbolicRef">The symbolic reference which target should be updated.</param>
        /// <param name = "targetRef">The new target.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual Reference UpdateTarget(Reference symbolicRef, Reference targetRef)
        {
            Ensure.ArgumentNotNull(symbolicRef, "symbolicRef");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            return UpdateTarget(symbolicRef, targetRef,
                (h, r) => Proxy.git_reference_set_target(h, r.CanonicalName));
        }

        private Reference UpdateTarget<T>(Reference reference, T target, Action<ReferenceSafeHandle, T> setter)
        {
            if (reference.CanonicalName == "HEAD")
            {
                if (target is ObjectId)
                {
                    return Add("HEAD", target as ObjectId, true);
                }

                if (target is DirectReference)
                {
                    return Add("HEAD", target as DirectReference, true);
                }

                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' is not a valid target type.", typeof(T)));
            }

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(reference.CanonicalName))
            {
                setter(referencePtr, target);
                return Reference.BuildFromPtr<Reference>(referencePtr, repo);
            }
        }

        internal ReferenceSafeHandle RetrieveReferencePtr(string referenceName, bool shouldThrowIfNotFound = true)
        {
            ReferenceSafeHandle reference = Proxy.git_reference_lookup(repo.Handle, referenceName, shouldThrowIfNotFound);

            return reference;
        }

        /// <summary>
        ///   Returns the list of references of the repository matching the specified <paramref name = "pattern" />.
        /// </summary>
        /// <param name = "pattern">The glob pattern the reference name should match.</param>
        /// <returns>A list of references, ready to be enumerated.</returns>
        public virtual IEnumerable<Reference> FromGlob(string pattern)
        {
            Ensure.ArgumentNotNullOrEmptyString(pattern, "pattern");

            return Proxy.git_reference_foreach_glob(repo.Handle, pattern, GitReferenceType.ListAll, Utf8Marshaler.FromNative)
                .OrderBy(name => name, StringComparer.Ordinal).Select(n => this[n]);
        }

        /// <summary>
        ///   Determines if the proposed reference name is well-formed.
        /// </summary>
        /// <para>
        ///   - Top-level names must contain only capital letters and underscores,
        ///   and must begin and end with a letter. (e.g. "HEAD", "ORIG_HEAD").
        ///
        ///   - Names prefixed with "refs/" can be almost anything.  You must avoid
        ///   the characters '~', '^', ':', '\\', '?', '[', and '*', and the
        ///   sequences ".." and "@{" which have special meaning to revparse.
        /// </para>
        /// <param name="canonicalName">The name to be checked.</param>
        /// <returns>true is the name is valid; false otherwise.</returns>
        public virtual bool IsValidName(string canonicalName)
        {
            return Proxy.git_reference_is_valid_name(canonicalName);
        }

        /// <summary>
        ///   Shortcut to return the HEAD reference.
        /// </summary>
        /// <returns>
        ///   A <see cref="DirectReference"/> if the HEAD is detached;
        ///   otherwise a <see cref="SymbolicReference"/>.
        /// </returns>
        public virtual Reference Head
        {
            get { return this["HEAD"]; }
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
