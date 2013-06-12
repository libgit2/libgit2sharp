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
            return Proxy.git_reference_list(repo.Handle)
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
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="DirectReference"/></param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite = false, string logMessage = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetId, "targetId");

            using (ReferenceSafeHandle handle = Proxy.git_reference_create(repo.Handle, name, targetId, allowOverwrite))
            {
                var newTarget = (DirectReference)Reference.BuildFromPtr<Reference>(handle, repo);

                LogReference(newTarget, targetId, logMessage);

                return newTarget;
            }
        }

        /// <summary>
        ///   Creates a symbolic reference  with the specified name and target
        /// </summary>
        /// <param name = "name">The canonical name of the reference to create.</param>
        /// <param name = "targetRef">The target reference.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="SymbolicReference"/></param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual SymbolicReference Add(string name, Reference targetRef, bool allowOverwrite = false, string logMessage = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            using (ReferenceSafeHandle handle = Proxy.git_reference_symbolic_create(repo.Handle, name, targetRef.CanonicalName, allowOverwrite))
            {
                var newTarget = (SymbolicReference)Reference.BuildFromPtr<Reference>(handle, repo);

                LogReference(newTarget, targetRef, logMessage);

                return newTarget;
            }
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
                using (ReferenceSafeHandle handle_out = Proxy.git_reference_rename(handle, newName, allowOverwrite))
                {
                    return Reference.BuildFromPtr<Reference>(handle_out, repo);
                }
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
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="directRef"/> reference</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId, string logMessage = null)
        {
            Ensure.ArgumentNotNull(directRef, "directRef");
            Ensure.ArgumentNotNull(targetId, "targetId");

            Reference newTarget = UpdateTarget(directRef, targetId,
                Proxy.git_reference_set_target);

            LogReference(directRef, targetId, logMessage);

            return newTarget;
        }

        /// <summary>
        ///   Updates the target of a symbolic reference.
        /// </summary>
        /// <param name = "symbolicRef">The symbolic reference which target should be updated.</param>
        /// <param name = "targetRef">The new target.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="symbolicRef"/> reference.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public virtual Reference UpdateTarget(Reference symbolicRef, Reference targetRef, string logMessage = null)
        {
            Ensure.ArgumentNotNull(symbolicRef, "symbolicRef");
            Ensure.ArgumentNotNull(targetRef, "targetRef");

            Reference newTarget = UpdateTarget(symbolicRef, targetRef,
                (h, r) => Proxy.git_reference_symbolic_set_target(h, r.CanonicalName));

            LogReference(symbolicRef, targetRef, logMessage);

            return newTarget;
        }

        private Reference UpdateTarget<T>(Reference reference, T target, Func<ReferenceSafeHandle, T, ReferenceSafeHandle> setter)
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

                if (target is SymbolicReference)
                {
                    return Add("HEAD", target as SymbolicReference, true);
                }

                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' is not a valid target type.", typeof(T)));
            }

            using (ReferenceSafeHandle referencePtr = RetrieveReferencePtr(reference.CanonicalName))
            {
                using (ReferenceSafeHandle ref_out = setter(referencePtr, target))
                {
                    return Reference.BuildFromPtr<Reference>(ref_out, repo);
                }
            }
        }

        private void LogReference(Reference reference, Reference target, string logMessage)
        {
            var directReference = target.ResolveToDirectReference();

            if (directReference == null)
            {
                return;
            }

            LogReference(reference, directReference.Target.Id, logMessage);
        }

        private void LogReference(Reference reference, ObjectId target, string logMessage)
        {
            if (string.IsNullOrEmpty(logMessage))
            {
                return;
            }

            repo.Refs.Log(reference).Append(target, logMessage);
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

            return Proxy.git_reference_foreach_glob(repo.Handle, pattern, Utf8Marshaler.FromNative)
                .Select(n => this[n]);
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

        /// <summary>
        ///   Returns as a <see cref="ReflogCollection"/> the reflog of the <see cref="Reference"/> named <paramref name="canonicalName"/>
        /// </summary>
        /// <param name="canonicalName">The canonical name of the reference</param>
        /// <returns>a <see cref="ReflogCollection"/>, enumerable of <see cref="ReflogEntry"/></returns>
        public virtual ReflogCollection Log(string canonicalName)
        {
            Ensure.ArgumentNotNullOrEmptyString(canonicalName, "canonicalName");

            return new ReflogCollection(repo, canonicalName);
        }

        /// <summary>
        ///   Returns as a <see cref="ReflogCollection"/> the reflog of the <see cref="Reference"/> <paramref name="reference"/>
        /// </summary>
        /// <param name="reference">The reference</param>
        /// <returns>a <see cref="ReflogCollection"/>, enumerable of <see cref="ReflogEntry"/></returns>
        public virtual ReflogCollection Log(Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

            return new ReflogCollection(repo, reference.CanonicalName);
        }

        /// <summary>
        /// Default implementation of direct reference rewriting, used by <see cref="RewriteHistory"/>.
        /// In addition to moving the reference to point to the new target, this makes a backup copy in
        /// "refs/original".
        /// </summary>
        /// <param name="oldRef">The reference to be rewritten.</param>
        /// <param name="newTarget">The target the new reference should point to.</param>
        /// <param name="namePrefix">Namespace for backing up refs. The default is "refs/original".</param>
        /// <returns>The newly-created reference.</returns>
        /// <exception cref="InvalidOperationException">This is thrown if the backup reference already exists.</exception>
        private Reference DefaultDirectReferenceRewriter(DirectReference oldRef, ObjectId newTarget, string namePrefix)
        {
            var newName = namePrefix + oldRef.CanonicalName.Substring("refs/".Length);

            if (Resolve<Reference>(newName) != null)
            {
                throw new InvalidOperationException(
                    String.Format("Can't back up reference '{0}' - '{1}' already exists", oldRef.CanonicalName, newName));
            }

            this.Add(newName, oldRef.TargetIdentifier, false, "filter-branch: backup");

            // Move the ref
            return UpdateTarget(oldRef, newTarget, "filter-branch: rewrite");
        }

        /// <summary>
        /// Rewrite some or all of the repository's commits and references
        /// </summary>
        /// <param name="commitsToRewrite">The <see cref="Commit"/>objects to rewrite</param>
        /// <param name="commitHeaderRewriter">Visitor for rewriting commit metadata</param>
        /// <param name="commitTreeRewriter">Visitor for rewriting commit trees</param>
        /// <param name="tagNameRewriter">Visitor for renaming tags. This is called with (OldTag.Name, OldTag.IsAnnotated, NewTarget).
        ///                               If it returns null or the empty string, the tag will not be changed.
        ///                               If it returns the input, the tag will be moved.
        ///                               Any other value results in a new tag.</param>
        /// <param name="parentRewriter">Visitor for mangling parent links</param>
        /// <param name="backupRefsNamespace">Namespace where to store the rewritten references (defaults to "refs/original/")</param>
        public virtual void RewriteHistory(
            IEnumerable<Commit> commitsToRewrite,
            Func<Commit, CommitRewriteInfo> commitHeaderRewriter = null,
            Func<Commit, TreeDefinition> commitTreeRewriter = null,
            Func<String, bool, GitObject, string> tagNameRewriter = null,
            Func<IEnumerable<Commit>, IEnumerable<Commit>> parentRewriter = null,
            string backupRefsNamespace = "refs/original/")
        {
            Ensure.ArgumentNotNull(commitsToRewrite, "commitsToRewrite");
            Ensure.ArgumentNotNullOrEmptyString(backupRefsNamespace, "backupRefsNamespace");

            if (!backupRefsNamespace.EndsWith("/"))
            {
                backupRefsNamespace += "/";
            }


            IList<Reference> originalRefs = this.ToList();
            if (originalRefs.Count == 0)
            {
                // Nothing to do
                return;
            }

            commitHeaderRewriter = commitHeaderRewriter ?? CommitRewriteInfo.From;
            commitTreeRewriter = commitTreeRewriter ?? TreeDefinition.From;
            parentRewriter = parentRewriter ?? (p => p);
            tagNameRewriter = tagNameRewriter ?? ((n, a, t) => null);

            // Find out which refs lead to at least one the commits
            var refsToRewrite = this.ReachableFrom(commitsToRewrite).ToList();

            var shaMap = new Dictionary<ObjectId, ObjectId>();
            var filter = new Filter
                             {
                                 Since = refsToRewrite,
                                 SortBy = GitSortOptions.Reverse | GitSortOptions.Topological
                             };

            var commits = repo.Commits.QueryBy(filter);
            foreach (var commit in commits)
            {
                // Get the new commit header
                var newHeader = commitHeaderRewriter(commit);

                // Get the new commit tree
                var newTreeDefinition = commitTreeRewriter(commit);
                var newTree = repo.ObjectDatabase.CreateTree(newTreeDefinition);

                // Find the new parents
                var newParents = commit.Parents
                                       .Select(oldParent =>
                                               shaMap.ContainsKey(oldParent.Id)
                                                   ? shaMap[oldParent.Id]
                                                   : oldParent.Id)
                                       .Select(id => repo.Lookup<Commit>(id));

                // Only allow rewriting of the parents if this commit was directly asked for
                if (commitsToRewrite.Contains(commit))
                    newParents = parentRewriter(newParents);

                // Create the new commit
                var newCommit = repo.ObjectDatabase.CreateCommit(newHeader.Message, newHeader.Author,
                                                                 newHeader.Committer, newTree,
                                                                 newParents);

                // Record the rewrite
                shaMap[commit.Id] = newCommit.Id;
            }

            // Rewrite the refs
            var refsToRollBack = new List<Reference>();
            var tagsToRollBack = new List<Tag>();
            try
            {
                // Ordering matters. In the case of `A -> B -> commit`, we need to make sure B is rewritten
                // before A.
                foreach (var reference in refsToRewrite.Where(x => !x.IsTag())
                                                       .OrderBy(ReferenceDepth))
                {
                    var dref = reference as DirectReference;
                    if (dref == null)
                    {
                        continue;
                    }

                    var newTarget = shaMap[dref.Target.Id];
                    var newDref = DefaultDirectReferenceRewriter(dref, newTarget, backupRefsNamespace);

                    if (newDref != dref)
                    {
                        refsToRollBack.Add(dref);
                    }
                }

                foreach (var tagref in refsToRewrite.Where(x => x.IsTag()))
                {
                    var oldTag = repo.Tags[tagref.CanonicalName];
                    var newTarget = RewriteTagAnnotationsDownToObject(shaMap, oldTag.Target);
                    var newTagName = tagNameRewriter(oldTag.Name, oldTag.IsAnnotated, newTarget);
                    if (!string.IsNullOrEmpty(newTagName))
                    {
                        var newTag = oldTag.IsAnnotated
                                         ? repo.Tags.Add(newTagName, newTarget, oldTag.Annotation.Tagger, oldTag.Annotation.Message, true)
                                         : repo.Tags.Add(newTagName, newTarget, true);
                        if (oldTag != newTag)
                        {
                            if (oldTag.IsAnnotated && newTag != null && newTag.Annotation != oldTag.Annotation)
                            {
                                shaMap[oldTag.Annotation.Id] = newTag.Annotation.Id;
                            }
                            tagsToRollBack.Add(oldTag);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Something went wrong. Roll back the rewrites
                foreach (var r in refsToRollBack)
                {
                    var dref = r as DirectReference;
                    if (dref != null)
                    {
                        repo.Refs.Add(dref.CanonicalName, dref.Target.Id, true, "filter-branch: abort");
                    }
                    else
                    {
                        repo.Refs.Add(r.CanonicalName, ((SymbolicReference)r).Target, true,
                                        "filter-branch: abort");
                    }
                }
                foreach (var t in tagsToRollBack)
                {
                    if (t.IsAnnotated)
                    {
                        repo.Tags.Add(t.CanonicalName, t.Annotation.Target, t.Annotation.Tagger, t.Annotation.Message,
                                      true);
                    }
                    else
                    {
                        repo.Tags.Add(t.Name, t.Target, true);
                    }
                }
                throw;
            }
        }

        private GitObject RewriteTagAnnotationsDownToObject(Dictionary<ObjectId, ObjectId> shaMap, GitObject oldTarget)
        {
            // Has this target already been rewritten?
            if (shaMap.ContainsKey(oldTarget.Id))
            {
                return repo.Lookup(shaMap[oldTarget.Id]);
            }

            var annotation = oldTarget as TagAnnotation;
            if (annotation == null)
            {
                //TODO: This is not covered by any test
                return shaMap.ContainsKey(oldTarget.Id) ? repo.Lookup(shaMap[oldTarget.Id]) : oldTarget;
            }

            // Recursively rewrite annotations if necessary
            var newTarget = RewriteTagAnnotationsDownToObject(shaMap, annotation.Target);
            var newAnnotation = repo.ObjectDatabase.CreateTag(annotation.Name, newTarget, annotation.Tagger,
                                                              annotation.Message);
            shaMap[annotation.Id] = newAnnotation.Id;
            return newAnnotation;
        }

        private int ReferenceDepth(Reference reference)
        {
            var dref = reference as DirectReference;
            return dref == null
                ? 1 + ReferenceDepth(((SymbolicReference)reference).Target)
                : 1;
        }
    }
}
