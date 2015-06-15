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
    /// The Collection of references in a <see cref="Repository"/>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ReferenceCollection : IEnumerable<Reference>
    {
        internal readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected ReferenceCollection()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        internal ReferenceCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        /// Gets the <see cref="LibGit2Sharp.Reference"/> with the specified name.
        /// </summary>
        /// <param name="name">The canonical name of the reference to resolve.</param>
        /// <returns>The resolved <see cref="LibGit2Sharp.Reference"/> if it has been found, null otherwise.</returns>
        public virtual Reference this[string name]
        {
            get { return Resolve<Reference>(name); }
        }

        #region IEnumerable<Reference> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Reference> GetEnumerator()
        {
            return Proxy.git_reference_list(repo.Handle)
                .Select(n => this[n])
                .GetEnumerator();
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
        /// Creates a direct or symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The name of the reference to create.</param>
        /// <param name="canonicalRefNameOrObjectish">The target which can be either the canonical name of a reference or a revparse spec.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="Reference"/></param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Add(string name, string canonicalRefNameOrObjectish,
            string logMessage)
        {
            return Add(name, canonicalRefNameOrObjectish, logMessage, false);
        }

        private enum RefState
        {
            Exists,
            DoesNotExistButLooksValid,
            DoesNotLookValid,
        }

        private static RefState TryResolveReference(out Reference reference, ReferenceCollection refsColl, string canonicalName)
        {
            if (!Reference.IsValidName(canonicalName))
            {
                reference = null;
                return RefState.DoesNotLookValid;
            }

            reference = refsColl[canonicalName];

            return reference != null ? RefState.Exists : RefState.DoesNotExistButLooksValid;
        }

        /// <summary>
        /// Creates a direct or symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The name of the reference to create.</param>
        /// <param name="canonicalRefNameOrObjectish">The target which can be either the canonical name of a reference or a revparse spec.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="Reference"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Add(string name, string canonicalRefNameOrObjectish, string logMessage, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(canonicalRefNameOrObjectish, "canonicalRefNameOrObjectish");

            Reference reference;
            RefState refState = TryResolveReference(out reference, this, canonicalRefNameOrObjectish);

            var gitObject = repo.Lookup(canonicalRefNameOrObjectish, GitObjectType.Any, LookUpOptions.None);

            if (refState == RefState.Exists)
            {
                return Add(name, reference, logMessage, allowOverwrite);
            }

            if (refState == RefState.DoesNotExistButLooksValid && gitObject == null)
            {
                using (ReferenceSafeHandle handle = Proxy.git_reference_symbolic_create(repo.Handle, name, canonicalRefNameOrObjectish, allowOverwrite,
                    logMessage))
                {
                    return Reference.BuildFromPtr<Reference>(handle, repo);
                }
            }

            Ensure.GitObjectIsNotNull(gitObject, canonicalRefNameOrObjectish);

            if (logMessage == null)
            {
                logMessage = string.Format(CultureInfo.InvariantCulture, "{0}: Created from {1}",
                    name.LooksLikeLocalBranch() ? "branch" : "reference", canonicalRefNameOrObjectish);
            }

            EnsureHasLog(name);
            return Add(name, gitObject.Id, logMessage, allowOverwrite);
        }


        /// <summary>
        /// Creates a direct or symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The name of the reference to create.</param>
        /// <param name="canonicalRefNameOrObjectish">The target which can be either the canonical name of a reference or a revparse spec.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Add(string name, string canonicalRefNameOrObjectish)
        {
            return Add(name, canonicalRefNameOrObjectish, null, false);
        }

        /// <summary>
        /// Creates a direct or symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The name of the reference to create.</param>
        /// <param name="canonicalRefNameOrObjectish">The target which can be either the canonical name of a reference or a revparse spec.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite)
        {
            return Add(name, canonicalRefNameOrObjectish, null, allowOverwrite);
        }
        /// <summary>
        /// Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetId">Id of the target object.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="DirectReference"/></param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, string logMessage)
        {
            return Add(name, targetId, logMessage, false);
        }

        /// <summary>
        /// Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetId">Id of the target object.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="DirectReference"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, string logMessage, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetId, "targetId");

            using (ReferenceSafeHandle handle = Proxy.git_reference_create(repo.Handle, name, targetId, allowOverwrite, logMessage))
            {
                return (DirectReference)Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetId">Id of the target object.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId)
        {
            return Add(name, targetId, null, false);
        }

        /// <summary>
        /// Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetId">Id of the target object.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite)
        {
            return Add(name, targetId, null, allowOverwrite);
        }

        /// <summary>
        /// Creates a symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetRef">The target reference.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="SymbolicReference"/></param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, string logMessage)
        {
            return Add(name, targetRef, logMessage, false);
        }

        /// <summary>
        /// Creates a symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetRef">The target reference.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="SymbolicReference"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, string logMessage, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            using (ReferenceSafeHandle handle = Proxy.git_reference_symbolic_create(repo.Handle,
                                                                                    name,
                                                                                    targetRef.CanonicalName,
                                                                                    allowOverwrite,
                                                                                    logMessage))
            {
                return (SymbolicReference)Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Creates a symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetRef">The target reference.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef)
        {
            return Add(name, targetRef, null, false);
        }

        /// <summary>
        /// Creates a symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetRef">The target reference.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, bool allowOverwrite)
        {
            return Add(name, targetRef, null, allowOverwrite);
        }

        /// <summary>
        /// Remove a reference with the specified name
        /// </summary>
        /// <param name="name">The canonical name of the reference to delete.</param>
        public virtual void Remove(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Reference reference = this[name];

            if (reference == null)
            {
                return;
            }

            Remove(reference);
        }

        /// <summary>
        /// Remove a reference from the repository
        /// </summary>
        /// <param name="reference">The reference to delete.</param>
        public virtual void Remove(Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

            Proxy.git_reference_remove(repo.Handle, reference.CanonicalName);
        }

        /// <summary>
        /// Rename an existing reference with a new name, and update the reflog
        /// </summary>
        /// <param name="reference">The reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="logMessage">Message added to the reflog.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(Reference reference, string newName, string logMessage)
        {
            return Rename(reference, newName, logMessage, false);
        }

        /// <summary>
        /// Rename an existing reference with a new name, and update the reflog
        /// </summary>
        /// <param name="reference">The reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="logMessage">Message added to the reflog.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(Reference reference, string newName, string logMessage, bool allowOverwrite)
        {
            Ensure.ArgumentNotNull(reference, "reference");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            if (logMessage == null)
            {
                logMessage = string.Format(CultureInfo.InvariantCulture,
                                           "{0}: renamed {1} to {2}",
                                           reference.IsLocalBranch
                                               ? "branch"
                                               : "reference",
                                           reference.CanonicalName,
                                           newName);
            }

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(reference.CanonicalName))
            using (ReferenceSafeHandle handle = Proxy.git_reference_rename(referencePtr, newName, allowOverwrite, logMessage))
            {
                return Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="currentName">The canonical name of the reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(string currentName, string newName)
        {
            return Rename(currentName, newName, null, false);
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="currentName">The canonical name of the reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(string currentName, string newName,
            bool allowOverwrite)
        {
            return Rename(currentName, newName, null, allowOverwrite);
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="currentName">The canonical name of the reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/></param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(string currentName, string newName,
            string logMessage)
        {
            return Rename(currentName, newName, logMessage, false);
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="currentName">The canonical name of the reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(string currentName, string newName,
            string logMessage, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "currentName");

            Reference reference = this[currentName];

            if (reference == null)
            {
                throw new LibGit2SharpException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Reference '{0}' doesn't exist. One cannot move a non existing reference.", currentName));
            }

            return Rename(reference, newName, logMessage, allowOverwrite);
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="reference">The reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(Reference reference, string newName)
        {
            return Rename(reference, newName, null, false);
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="reference">The reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Rename(Reference reference, string newName, bool allowOverwrite)
        {
            return Rename(reference, newName, null, allowOverwrite);
        }

        internal T Resolve<T>(string name) where T : Reference
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(name, false))
            {
                return referencePtr == null
                    ? null
                    : Reference.BuildFromPtr<T>(referencePtr, repo);
            }
        }

        /// <summary>
        /// Updates the target of a direct reference.
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="targetId">The new target.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="directRef"/> reference</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId, string logMessage)
        {
            Ensure.ArgumentNotNull(directRef, "directRef");
            Ensure.ArgumentNotNull(targetId, "targetId");

            if (directRef.CanonicalName == "HEAD")
            {
                return UpdateHeadTarget(targetId, logMessage);
            }

            return UpdateDirectReferenceTarget(directRef, targetId, logMessage);
        }

        private Reference UpdateDirectReferenceTarget(Reference directRef, ObjectId targetId, string logMessage)
        {
            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(directRef.CanonicalName))
            using (ReferenceSafeHandle handle = Proxy.git_reference_set_target(referencePtr, targetId, logMessage))
            {
                return Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Updates the target of a direct reference.
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="objectish">The revparse spec of the target.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/></param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference directRef, string objectish, string logMessage)
        {
            Ensure.ArgumentNotNull(directRef, "directRef");
            Ensure.ArgumentNotNull(objectish, "objectish");

            GitObject target = repo.Lookup(objectish);

            Ensure.GitObjectIsNotNull(target, objectish);

            return UpdateTarget(directRef, target.Id, logMessage);
        }

        /// <summary>
        /// Updates the target of a direct reference
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="objectish">The revparse spec of the target.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference directRef, string objectish)
        {
            return UpdateTarget(directRef, objectish, null);
        }

        /// <summary>
        /// Updates the target of a reference
        /// </summary>
        /// <param name="name">The canonical name of the reference.</param>
        /// <param name="canonicalRefNameOrObjectish">The target which can be either the canonical name of a reference or a revparse spec.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="name"/> reference.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(string name, string canonicalRefNameOrObjectish, string logMessage)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(canonicalRefNameOrObjectish, "canonicalRefNameOrObjectish");

            if (name == "HEAD")
            {
                return UpdateHeadTarget(canonicalRefNameOrObjectish, logMessage);
            }

            Reference reference = this[name];

            var directReference = reference as DirectReference;
            if (directReference != null)
            {
                return UpdateTarget(directReference, canonicalRefNameOrObjectish, logMessage);
            }

            var symbolicReference = reference as SymbolicReference;
            if (symbolicReference != null)
            {
                Reference targetRef;

                RefState refState = TryResolveReference(out targetRef, this, canonicalRefNameOrObjectish);

                if (refState == RefState.DoesNotLookValid)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The reference specified by {0} is a Symbolic reference, you must provide a reference canonical name as the target.", name), "canonicalRefNameOrObjectish");
                }

                return UpdateTarget(symbolicReference, targetRef, logMessage);
            }

            throw new LibGit2SharpException(CultureInfo.InvariantCulture,
                                            "Reference '{0}' has an unexpected type ('{1}').",
                                            name,
                                            reference.GetType());
        }

        /// <summary>
        /// Updates the target of a reference
        /// </summary>
        /// <param name="name">The canonical name of the reference.</param>
        /// <param name="canonicalRefNameOrObjectish">The target which can be either the canonical name of a reference or a revparse spec.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(string name, string canonicalRefNameOrObjectish)
        {
            return UpdateTarget(name, canonicalRefNameOrObjectish, null);
        }

        /// <summary>
        /// Updates the target of a direct reference
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="targetId">The new target.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId)
        {
            return UpdateTarget(directRef, targetId, null);
        }

        /// <summary>
        /// Updates the target of a symbolic reference
        /// </summary>
        /// <param name="symbolicRef">The symbolic reference which target should be updated.</param>
        /// <param name="targetRef">The new target.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="symbolicRef"/> reference.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference symbolicRef, Reference targetRef, string logMessage)
        {
            Ensure.ArgumentNotNull(symbolicRef, "symbolicRef");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            if (symbolicRef.CanonicalName == "HEAD")
            {
                return UpdateHeadTarget(targetRef, logMessage);
            }

            return UpdateSymbolicRefenceTarget(symbolicRef, targetRef, logMessage);
        }

        private Reference UpdateSymbolicRefenceTarget(Reference symbolicRef, Reference targetRef, string logMessage)
        {
            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(symbolicRef.CanonicalName))
            using (ReferenceSafeHandle handle = Proxy.git_reference_symbolic_set_target(referencePtr, targetRef.CanonicalName, logMessage))
            {
                return Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Updates the target of a symbolic reference
        /// </summary>
        /// <param name="symbolicRef">The symbolic reference which target should be updated.</param>
        /// <param name="targetRef">The new target.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference symbolicRef, Reference targetRef)
        {
            return UpdateTarget(symbolicRef, targetRef, null);
        }

        internal Reference MoveHeadTarget<T>(T target)
        {
            if (target is ObjectId)
            {
                Proxy.git_repository_set_head_detached(repo.Handle, target as ObjectId);
            }
            else if (target is DirectReference || target is SymbolicReference)
            {
                Proxy.git_repository_set_head(repo.Handle, (target as Reference).CanonicalName);
            }
            else if (target is string)
            {
                var targetIdentifier = target as string;

                if (Reference.IsValidName(targetIdentifier) && targetIdentifier.LooksLikeLocalBranch())
                {
                    Proxy.git_repository_set_head(repo.Handle, targetIdentifier);
                }
                else
                {
                    using (var annotatedCommit = Proxy.git_annotated_commit_from_revspec(repo.Handle, targetIdentifier))
                    {
                        Proxy.git_repository_set_head_detached_from_annotated(repo.Handle, annotatedCommit);
                    }
                }
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                                          "'{0}' is not a valid target type.",
                                                          typeof(T)));
            }

            return repo.Refs.Head;
        }

        internal Reference UpdateHeadTarget(ObjectId target, string logMessage)
        {
            Add("HEAD", target, logMessage, true);

            return repo.Refs.Head;
        }

        internal Reference UpdateHeadTarget(Reference target, string logMessage)
        {
            Ensure.ArgumentConformsTo(target, r => (r is DirectReference || r is SymbolicReference), "target");

            Add("HEAD", target, logMessage, true);

            return repo.Refs.Head;
        }

        internal Reference UpdateHeadTarget(string target, string logMessage)
        {
            this.Add("HEAD", target, logMessage, true);

            return repo.Refs.Head;
        }

        internal ReferenceSafeHandle RetrieveReferencePtr(string referenceName, bool shouldThrowIfNotFound = true)
        {
            ReferenceSafeHandle reference = Proxy.git_reference_lookup(repo.Handle, referenceName, shouldThrowIfNotFound);

            return reference;
        }

        /// <summary>
        /// Returns the list of references of the repository matching the specified <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">The glob pattern the reference name should match.</param>
        /// <returns>A list of references, ready to be enumerated.</returns>
        public virtual IEnumerable<Reference> FromGlob(string pattern)
        {
            Ensure.ArgumentNotNullOrEmptyString(pattern, "pattern");

            return Proxy.git_reference_foreach_glob(repo.Handle, pattern, LaxUtf8Marshaler.FromNative)
                .Select(n => this[n]);
        }

        /// <summary>
        /// Shortcut to return the HEAD reference.
        /// </summary>
        /// <returns>
        /// A <see cref="DirectReference"/> if the HEAD is detached;
        /// otherwise a <see cref="SymbolicReference"/>.
        /// </returns>
        public virtual Reference Head
        {
            get { return this["HEAD"]; }
        }


        /// <summary>
        /// Find the <see cref="Reference"/>s among <paramref name="refSubset"/>
        /// that can reach at least one <see cref="Commit"/> in the specified <paramref name="targets"/>.
        /// </summary>
        /// <param name="refSubset">The set of <see cref="Reference"/>s to examine.</param>
        /// <param name="targets">The set of <see cref="Commit"/>s that are interesting.</param>
        /// <returns>A subset of <paramref name="refSubset"/> that can reach at least one <see cref="Commit"/> within <paramref name="targets"/>.</returns>
        public virtual IEnumerable<Reference> ReachableFrom(
            IEnumerable<Reference> refSubset,
            IEnumerable<Commit> targets)
        {
            Ensure.ArgumentNotNull(refSubset, "refSubset");
            Ensure.ArgumentNotNull(targets, "targets");

            var refs = new List<Reference>(refSubset);
            if (refs.Count == 0)
            {
                return Enumerable.Empty<Reference>();
            }

            List<ObjectId> targetsSet = targets.Select(c => c.Id).Distinct().ToList();
            if (targetsSet.Count == 0)
            {
                return Enumerable.Empty<Reference>();
            }

            var result = new List<Reference>();

            foreach (var reference in refs)
            {
                var peeledTargetCommit = reference
                                            .ResolveToDirectReference()
                                            .Target.DereferenceToCommit(false);

                if (peeledTargetCommit == null)
                {
                    continue;
                }

                var commitId = peeledTargetCommit.Id;

                foreach (var potentialAncestorId in targetsSet)
                {
                    if (potentialAncestorId == commitId)
                    {
                        result.Add(reference);
                        break;
                    }

                    if (Proxy.git_graph_descendant_of(repo.Handle, commitId, potentialAncestorId))
                    {
                        result.Add(reference);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Find the <see cref="Reference"/>s
        /// that can reach at least one <see cref="Commit"/> in the specified <paramref name="targets"/>.
        /// </summary>
        /// <param name="targets">The set of <see cref="Commit"/>s that are interesting.</param>
        /// <returns>The list of <see cref="Reference"/> that can reach at least one <see cref="Commit"/> within <paramref name="targets"/>.</returns>
        public virtual IEnumerable<Reference> ReachableFrom(IEnumerable<Commit> targets)
        {
            return ReachableFrom(this, targets);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count());
            }
        }

        /// <summary>
        /// Returns as a <see cref="ReflogCollection"/> the reflog of the <see cref="Reference"/> named <paramref name="canonicalName"/>
        /// </summary>
        /// <param name="canonicalName">The canonical name of the reference</param>
        /// <returns>a <see cref="ReflogCollection"/>, enumerable of <see cref="ReflogEntry"/></returns>
        public virtual ReflogCollection Log(string canonicalName)
        {
            Ensure.ArgumentNotNullOrEmptyString(canonicalName, "canonicalName");

            return new ReflogCollection(repo, canonicalName);
        }

        /// <summary>
        /// Returns as a <see cref="ReflogCollection"/> the reflog of the <see cref="Reference"/> <paramref name="reference"/>
        /// </summary>
        /// <param name="reference">The reference</param>
        /// <returns>a <see cref="ReflogCollection"/>, enumerable of <see cref="ReflogEntry"/></returns>
        public virtual ReflogCollection Log(Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

            return new ReflogCollection(repo, reference.CanonicalName);
        }

        /// <summary>
        /// Rewrite some of the commits in the repository and all the references that can reach them.
        /// </summary>
        /// <param name="options">Specifies behavior for this rewrite.</param>
        /// <param name="commitsToRewrite">The <see cref="Commit"/> objects to rewrite.</param>
        public virtual void RewriteHistory(RewriteHistoryOptions options, params Commit[] commitsToRewrite)
        {
            Ensure.ArgumentNotNull(commitsToRewrite, "commitsToRewrite");

            RewriteHistory(options, commitsToRewrite.AsEnumerable());
        }

        /// <summary>
        /// Rewrite some of the commits in the repository and all the references that can reach them.
        /// </summary>
        /// <param name="options">Specifies behavior for this rewrite.</param>
        /// <param name="commitsToRewrite">The <see cref="Commit"/> objects to rewrite.</param>
        public virtual void RewriteHistory(RewriteHistoryOptions options, IEnumerable<Commit> commitsToRewrite)
        {
            Ensure.ArgumentNotNull(commitsToRewrite, "commitsToRewrite");
            Ensure.ArgumentNotNull(options, "options");
            Ensure.ArgumentNotNullOrEmptyString(options.BackupRefsNamespace, "options.BackupRefsNamespace");

            IList<Reference> originalRefs = this.ToList();
            if (originalRefs.Count == 0)
            {
                // Nothing to do
                return;
            }

            var historyRewriter = new HistoryRewriter(repo, commitsToRewrite, options);

            historyRewriter.Execute();
        }

        /// <summary>
        /// Ensure that a reflog exists for the given canonical name
        /// </summary>
        /// <param name="canonicalName">Canonical name of the reference</param>
        internal void EnsureHasLog(string canonicalName)
        {
            Proxy.git_reference_ensure_log(repo.Handle, canonicalName);
        }
    }
}
