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
        /// Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetId">Id of the target object.</param>
        /// <param name="signature">Identity used for updating the reflog.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="DirectReference"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, Signature signature, string logMessage, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetId, "targetId");

            using (ReferenceSafeHandle handle = Proxy.git_reference_create(repo.Handle, name, targetId, allowOverwrite, signature.OrDefault(repo.Config), logMessage))
            {
                return (DirectReference)Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetId">Id of the target object.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite = false)
        {
            return Add(name, targetId, null, null, allowOverwrite);
        }

        /// <summary>
        /// Creates a direct reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetId">Id of the target object.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="DirectReference"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        [Obsolete("This method will be removed in the next release. Prefer the overload that takes a signature and a message for the reflog.")]
        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite, string logMessage)
        {
            return Add(name, targetId, null, logMessage, allowOverwrite);
        }

        /// <summary>
        /// Creates a symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetRef">The target reference.</param>
        /// <param name="signature">Identity used for updating the reflog.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="SymbolicReference"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, Signature signature, string logMessage, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            using (ReferenceSafeHandle handle = Proxy.git_reference_symbolic_create(repo.Handle, name, targetRef.CanonicalName,
                allowOverwrite, signature.OrDefault(repo.Config), logMessage))
            {
                return (SymbolicReference)Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Creates a symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetRef">The target reference.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, bool allowOverwrite = false)
        {
            return Add(name, targetRef, null, null, allowOverwrite);
        }

        /// <summary>
        /// Creates a symbolic reference with the specified name and target
        /// </summary>
        /// <param name="name">The canonical name of the reference to create.</param>
        /// <param name="targetRef">The target reference.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="SymbolicReference"/></param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        [Obsolete("This method will be removed in the next release. Prefer the overload that takes a signature and a message for the reflog.")]
        public virtual SymbolicReference Add(string name, Reference targetRef, bool allowOverwrite, string logMessage)
        {
            return Add(name, targetRef, null, logMessage, allowOverwrite);
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
        /// <param name="signature">Identity used for updating the reflog.</param>
        /// <param name="logMessage">Message added to the reflog.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Move(Reference reference, string newName, Signature signature, string logMessage = null, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNull(reference, "reference");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            if (logMessage == null)
            {
                logMessage = string.Format("{0}: renamed {1} to {2}",
                    reference.IsLocalBranch() ? "branch" : "reference", reference.CanonicalName, newName);
            }

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(reference.CanonicalName))
            using (ReferenceSafeHandle handle = Proxy.git_reference_rename(referencePtr, newName, allowOverwrite, signature.OrDefault(repo.Config), logMessage))
            {
                return Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="reference">The reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference Move(Reference reference, string newName, bool allowOverwrite = false)
        {
            return Move(reference, newName, null, null, allowOverwrite);
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
        /// Updates the target of a direct reference.
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="targetId">The new target.</param>
        /// <param name="signature">The identity used for updating the reflog.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="directRef"/> reference</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId, Signature signature, string logMessage)
        {
            Ensure.ArgumentNotNull(directRef, "directRef");
            Ensure.ArgumentNotNull(targetId, "targetId");

            signature = signature.OrDefault(repo.Config);

            if (directRef.CanonicalName == "HEAD")
            {
                return UpdateHeadTarget(targetId, signature, logMessage);
            }

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(directRef.CanonicalName))
            using (ReferenceSafeHandle handle = Proxy.git_reference_set_target(referencePtr, targetId, signature, logMessage))
            {
                return Reference.BuildFromPtr<Reference>(handle, repo);
            }
        }

        /// <summary>
        /// Updates the target of a direct reference
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="targetId">The new target.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId)
        {
            return UpdateTarget(directRef, targetId, null, null);
        }

        /// <summary>
        /// Updates the target of a direct reference
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="targetId">The new target.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="directRef"/> reference</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        [Obsolete("This method will be removed in the next release. Prefer the overload that takes a signature and a message for the reflog.")]
        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId, string logMessage)
        {
            return UpdateTarget(directRef, targetId, null, logMessage);
        }

        /// <summary>
        /// Updates the target of a symbolic reference
        /// </summary>
        /// <param name="symbolicRef">The symbolic reference which target should be updated.</param>
        /// <param name="targetRef">The new target.</param>
        /// <param name="signature">The identity used for updating the reflog.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="symbolicRef"/> reference.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public virtual Reference UpdateTarget(Reference symbolicRef, Reference targetRef, Signature signature, string logMessage)
        {
            Ensure.ArgumentNotNull(symbolicRef, "symbolicRef");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            signature = signature.OrDefault(repo.Config);

            if (symbolicRef.CanonicalName == "HEAD")
            {
                return UpdateHeadTarget(targetRef, signature, logMessage);
            }

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(symbolicRef.CanonicalName))
            using (ReferenceSafeHandle handle = Proxy.git_reference_symbolic_set_target(referencePtr, targetRef.CanonicalName, signature, logMessage))
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
            return UpdateTarget(symbolicRef, targetRef, null, null);
        }

        /// <summary>
        /// Updates the target of a symbolic reference
        /// </summary>
        /// <param name="symbolicRef">The symbolic reference which target should be updated.</param>
        /// <param name="targetRef">The new target.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="symbolicRef"/> reference.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        [Obsolete("This method will be removed in the next release. Prefer the overload that takes a signature and a message for the reflog.")]
        public virtual Reference UpdateTarget(Reference symbolicRef, Reference targetRef, string logMessage)
        {
            return UpdateTarget(symbolicRef, targetRef, null, logMessage);
        }

        internal Reference UpdateHeadTarget<T>(T target, Signature signature, string logMessage)
        {
            Debug.Assert(signature != null);

            if (target is ObjectId)
            {
                Proxy.git_repository_set_head_detached(repo.Handle, target as ObjectId, signature, logMessage);
            }
            else if (target is DirectReference || target is SymbolicReference)
            {
                Proxy.git_repository_set_head(repo.Handle, (target as Reference).CanonicalName, signature, logMessage);
            }
            else if (target is string)
            {
                var targetIdentifier = target as string;

                if (IsValidName(targetIdentifier))
                {
                    Proxy.git_repository_set_head(repo.Handle, targetIdentifier, signature, logMessage);
                }
                else
                {
                    GitObject commit = repo.Lookup(targetIdentifier,
                        GitObjectType.Any,
                        LookUpOptions.ThrowWhenNoGitObjectHasBeenFound |
                        LookUpOptions.DereferenceResultToCommit |
                        LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit);

                    Proxy.git_repository_set_head_detached(repo.Handle, commit.Id, signature, logMessage);
                }
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' is not a valid target type.", typeof (T)));
            }

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
        /// Determines if the proposed reference name is well-formed.
        /// </summary>
        /// <para>
        /// - Top-level names must contain only capital letters and underscores,
        /// and must begin and end with a letter. (e.g. "HEAD", "ORIG_HEAD").
        ///
        /// - Names prefixed with "refs/" can be almost anything.  You must avoid
        /// the characters '~', '^', ':', '\\', '?', '[', and '*', and the
        /// sequences ".." and "@{" which have special meaning to revparse.
        /// </para>
        /// <param name="canonicalName">The name to be checked.</param>
        /// <returns>true is the name is valid; false otherwise.</returns>
        public virtual bool IsValidName(string canonicalName)
        {
            return Proxy.git_reference_is_valid_name(canonicalName);
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

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", this.Count());
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
