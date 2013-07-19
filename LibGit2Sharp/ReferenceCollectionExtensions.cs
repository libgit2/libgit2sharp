using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="ReferenceCollection"/>.
    /// </summary>
    public static class ReferenceCollectionExtensions
    {
        private enum RefState
        {
            Exists,
            DoesNotExistButLooksValid,
            DoesNotLookValid,
        }

        private static RefState TryResolveReference(out Reference reference, ReferenceCollection refsColl, string canonicalName)
        {
            if (!refsColl.IsValidName(canonicalName))
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
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <param name="refsColl">The <see cref="ReferenceCollection"/> being worked with.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> when adding the <see cref="Reference"/></param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public static Reference Add(this ReferenceCollection refsColl, string name, string canonicalRefNameOrObjectish, bool allowOverwrite = false, string logMessage = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(canonicalRefNameOrObjectish, "canonicalRefNameOrObjectish");

            Reference reference;
            RefState refState = TryResolveReference(out reference, refsColl, canonicalRefNameOrObjectish);

            var gitObject = refsColl.repo.Lookup(canonicalRefNameOrObjectish, GitObjectType.Any, LookUpOptions.None);

            if (refState == RefState.Exists)
            {
                return refsColl.Add(name, reference, allowOverwrite, logMessage);
            }

            if (refState == RefState.DoesNotExistButLooksValid && gitObject == null)
            {
                using (ReferenceSafeHandle handle = Proxy.git_reference_symbolic_create(refsColl.repo.Handle, name, canonicalRefNameOrObjectish, allowOverwrite))
                {
                    return Reference.BuildFromPtr<Reference>(handle, refsColl.repo);
                }
            }

            Ensure.GitObjectIsNotNull(gitObject, canonicalRefNameOrObjectish);

            return refsColl.Add(name, gitObject.Id, allowOverwrite, logMessage);
        }
        /// <summary>
        /// Updates the target of a direct reference.
        /// </summary>
        /// <param name="directRef">The direct reference which target should be updated.</param>
        /// <param name="objectish">The revparse spec of the target.</param>
        /// <param name="refsColl">The <see cref="ReferenceCollection"/> being worked with.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="directRef"/> reference.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public static Reference UpdateTarget(this ReferenceCollection refsColl, Reference directRef, string objectish, string logMessage = null)
        {
            Ensure.ArgumentNotNull(directRef, "directRef");
            Ensure.ArgumentNotNull(objectish, "objectish");

            GitObject target = refsColl.repo.Lookup(objectish);

            Ensure.GitObjectIsNotNull(target, objectish);

            return refsColl.UpdateTarget(directRef, target.Id, logMessage);
        }

        /// <summary>
        /// Rename an existing reference with a new name
        /// </summary>
        /// <param name="currentName">The canonical name of the reference to rename.</param>
        /// <param name="newName">The new canonical name.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <param name="refsColl">The <see cref="ReferenceCollection"/> being worked with.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public static Reference Move(this ReferenceCollection refsColl, string currentName, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "currentName");

            Reference reference = refsColl[currentName];

            if (reference == null)
            {
                throw new LibGit2SharpException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Reference '{0}' doesn't exist. One cannot move a non existing reference.", currentName));
            }

            return refsColl.Move(reference, newName, allowOverwrite);
        }

        /// <summary>
        /// Updates the target of a reference.
        /// </summary>
        /// <param name="name">The canonical name of the reference.</param>
        /// <param name="canonicalRefNameOrObjectish">The target which can be either the canonical name of a reference or a revparse spec.</param>
        /// <param name="refsColl">The <see cref="ReferenceCollection"/> being worked with.</param>
        /// <param name="logMessage">The optional message to log in the <see cref="ReflogCollection"/> of the <paramref name="name"/> reference.</param>
        /// <returns>A new <see cref="Reference"/>.</returns>
        public static Reference UpdateTarget(this ReferenceCollection refsColl, string name, string canonicalRefNameOrObjectish, string logMessage = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(canonicalRefNameOrObjectish, "canonicalRefNameOrObjectish");

            if (name == "HEAD")
            {
                return refsColl.Add("HEAD", canonicalRefNameOrObjectish, true);
            }

            Reference reference = refsColl[name];

            var directReference = reference as DirectReference;
            if (directReference != null)
            {
                return refsColl.UpdateTarget(directReference, canonicalRefNameOrObjectish, logMessage);
            }

            var symbolicReference = reference as SymbolicReference;
            if (symbolicReference != null)
            {
                Reference targetRef;

                RefState refState = TryResolveReference(out targetRef, refsColl, canonicalRefNameOrObjectish);

                if (refState == RefState.DoesNotLookValid)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The reference specified by {0} is a Symbolic reference, you must provide a reference canonical name as the target.", name), "canonicalRefNameOrObjectish");
                }

                return refsColl.UpdateTarget(symbolicReference, targetRef, logMessage);
            }

            throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Reference '{0}' has an unexpected type ('{1}').", name, reference.GetType()));
        }

        /// <summary>
        /// Delete a reference with the specified name
        /// </summary>
        /// <param name="refsColl">The <see cref="ReferenceCollection"/> being worked with.</param>
        /// <param name="name">The canonical name of the reference to delete.</param>
        public static void Remove(this ReferenceCollection refsColl, string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Reference reference = refsColl[name];

            if (reference == null)
            {
                return;
            }

            refsColl.Remove(reference);
        }

        /// <summary>
        /// Find the <see cref="Reference"/>s among <paramref name="refSubset"/>
        /// that can reach at least one <see cref="Commit"/> in the specified <paramref name="targets"/>.
        /// </summary>
        /// <param name="refsColl">The <see cref="ReferenceCollection"/> being worked with.</param>
        /// <param name="refSubset">The set of <see cref="Reference"/>s to examine.</param>
        /// <param name="targets">The set of <see cref="Commit"/>s that are interesting.</param>
        /// <returns>A subset of <paramref name="refSubset"/> that can reach at least one <see cref="Commit"/> within <paramref name="targets"/>.</returns>
        public static IEnumerable<Reference> ReachableFrom(
            this ReferenceCollection refsColl,
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

            var targetsSet = new HashSet<Commit>(targets);
            if (targetsSet.Count == 0)
            {
                return Enumerable.Empty<Reference>();
            }

            var allCommits = refsColl.repo.Commits;

            var result = new List<Reference>();

            foreach (var reference in refs)
            {
                foreach (var commit in allCommits.QueryBy(new CommitFilter { Since = reference }))
                {
                    if (!targetsSet.Contains(commit))
                    {
                        continue;
                    }

                    result.Add(reference);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Find the <see cref="Reference"/>s
        /// that can reach at least one <see cref="Commit"/> in the specified <paramref name="targets"/>.
        /// </summary>
        /// <param name="refsColl">The <see cref="ReferenceCollection"/> being worked with.</param>
        /// <param name="targets">The set of <see cref="Commit"/>s that are interesting.</param>
        /// <returns>The list of <see cref="Reference"/> that can reach at least one <see cref="Commit"/> within <paramref name="targets"/>.</returns>
        public static IEnumerable<Reference> ReachableFrom(
            this ReferenceCollection refsColl,
            IEnumerable<Commit> targets)
        {
            return ReachableFrom(refsColl, refsColl, targets);
        }
    }
}
