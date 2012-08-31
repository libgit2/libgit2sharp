using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The Collection of references in a <see cref = "Repository" />
    /// </summary>
    public class ReferenceCollection : IEnumerable<Reference>
    {
        private readonly Repository repo;

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

        private enum RefState
        {
            Exists,
            DoesNotExistButLooksValid,
            DoesNotLookValid,
        }

        private RefState TryResolveReference(out Reference reference, string canonicalName)
        {
            try
            {
                //TODO: Maybe would it be better to rather rely on git_reference_normalize_name()
                //This would be much more straightforward and less subject to fail for the wrong reason.

                reference = repo.Refs[canonicalName];

                if (reference != null)
                {
                    return RefState.Exists;
                }

                return RefState.DoesNotExistButLooksValid;
            }
            catch (LibGit2SharpException)
            {
                reference = null;
                return RefState.DoesNotLookValid;
            }
        }

        /// <summary>
        ///   Creates a direct or symbolic reference with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "canonicalRefNameOrObjectish">The target which can be either the canonical name of a branch reference or a revparse spec.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual Reference Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(canonicalRefNameOrObjectish, "canonicalRefNameOrObjectish");

            Reference reference;
            RefState refState = TryResolveReference(out reference, canonicalRefNameOrObjectish);

            var gitObject = repo.Lookup(canonicalRefNameOrObjectish, GitObjectType.Any, LookUpOptions.None);

            if (refState == RefState.Exists || (refState == RefState.DoesNotExistButLooksValid && gitObject == null))
            {
                using (ReferenceSafeHandle handle = CreateSymbolicReference(name, canonicalRefNameOrObjectish, allowOverwrite))
                {
                    return Reference.BuildFromPtr<Reference>(handle, repo);
                }
            }

            return Add(name, gitObject.Id, allowOverwrite);
        }

        /// <summary>
        ///   Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "targetId">Id of the target object.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetId, "targetId");

            using (ReferenceSafeHandle handle = CreateDirectReference(name, targetId, allowOverwrite))
            {
                return (DirectReference)Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        ///   Creates a symbolic reference  with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "targetRef">The target reference.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            using (ReferenceSafeHandle handle = CreateSymbolicReference(name, targetRef.CanonicalName, allowOverwrite))
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
            return Add(name, target, allowOverwrite);
        }

        private ReferenceSafeHandle CreateSymbolicReference(string name, string target, bool allowOverwrite)
        {
            return Proxy.git_reference_create_symbolic(repo.Handle, name, target, allowOverwrite);
        }

        private ReferenceSafeHandle CreateDirectReference(string name, ObjectId targetId, bool allowOverwrite)
        {
            return Proxy.git_reference_create_oid(repo.Handle, name, targetId, allowOverwrite);
        }

        /// <summary>
        ///   Delete a reference with the specified name
        /// </summary>
        /// <param name = "name">The name of the reference to delete.</param>
        public virtual void Remove(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            using (ReferenceSafeHandle handle = RetrieveReferencePtr(name))
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
            Remove(name);
        }

        /// <summary>
        ///   Rename an existing reference with a new name
        /// </summary>
        /// <param name = "currentName">The canonical name of the reference to rename.</param>
        /// <param name = "newName">The new canonical name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns></returns>
        public virtual Reference Move(string currentName, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "currentName");

            Reference reference = this[currentName];

            if (reference == null)
            {
                throw new LibGit2SharpException(string.Format("Reference '{0}' doesn't exist. One cannot move a non existing reference.", currentName));
            }

            return Move(reference, newName, allowOverwrite);
        }

        /// <summary>
        ///   Rename an existing reference with a new name
        /// </summary>
        /// <param name = "reference">The reference to rename.</param>
        /// <param name = "newName">The new canonical name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns></returns>
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

                throw new ArgumentException(string.Format("'{0}' is not a valid target type.", typeof(T)));
            }

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(reference.CanonicalName))
            {
                setter(referencePtr, target);
                return Reference.BuildFromPtr<Reference>(referencePtr, repo);
            }
        }

        /// <summary>
        ///   Updates the target of a reference.
        /// </summary>
        /// <param name = "name">The name of the reference.</param>
        /// <param name = "target">The target which can be either a sha or the name of another reference.</param>
        public virtual Reference UpdateTarget(string name, string target)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            if (name == "HEAD")
            {
                return Add("HEAD", target, true);
            }

            Reference reference = this[name];

            var directReference = reference as DirectReference;
            if (directReference != null)
            {
                ObjectId id;
                bool isObjectIdentifier = ObjectId.TryParse(target, out id);

                if (!isObjectIdentifier)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The reference specified by {0} is an Oid reference, you must provide a sha as the target.", name), "target");
                }


                return UpdateTarget(directReference, id);
            }

            var symbolicReference = reference as SymbolicReference;
            if (symbolicReference != null)
            {
                Reference targetRef = this[target];

                if (targetRef == null)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The reference specified by {0} is a Symbolic reference, you must provide a reference canonical name as the target.", name), "target");
                }

                return UpdateTarget(symbolicReference, targetRef);
            }

            throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Reference '{0}' has an unexpected type ('{1}').", name, reference.GetType()));
        }

        private ReferenceSafeHandle RetrieveReferencePtr(string referenceName, bool shouldThrowIfNotFound = true)
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

            return new GlobedReferenceEnumerable(repo.Handle, pattern).Select(n => this[n]);
        }

        private class GlobedReferenceEnumerable : IEnumerable<string>
        {
            private readonly List<string> list = new List<string>();

            public GlobedReferenceEnumerable(RepositorySafeHandle handle, string pattern)
            {
                Proxy.git_reference_foreach_glob(handle, pattern, GitReferenceType.ListAll, Callback);
                list.Sort(StringComparer.Ordinal);
            }

            private int Callback(IntPtr branchName, IntPtr payload)
            {
                string name = Utf8Marshaler.FromNative(branchName);
                list.Add(name);
                return 0;
            }

            public IEnumerator<string> GetEnumerator()
            {
                return list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
