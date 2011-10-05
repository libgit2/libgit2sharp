using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The Collection of references in a <see cref = "Repository" />
    /// </summary>
    public class ReferenceCollection : IEnumerable<Reference>
    {
        private readonly Repository repo;

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
        public Reference this[string name]
        {
            get { return Resolve<Reference>(name); }
        }

        #region IEnumerable<Reference> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Reference> GetEnumerator()
        {
            return Libgit2UnsafeHelper
                .ListAllReferenceNames(repo.Handle, GitReferenceType.ListAll)
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
        ///   Creates a direct or symbolic reference with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "target">The target which can be either a sha or the canonical name of another reference.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public Reference Create(string name, string target, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            ObjectId id;

            IntPtr reference;
            int res;

            if (ObjectId.TryParse(target, out id))
            {
                res = CreateDirectReference(name, id, allowOverwrite, out reference);
            }
            else
            {
                res = CreateSymbolicReference(name, target, allowOverwrite, out reference);
            }

            Ensure.Success(res);

            return Reference.BuildFromPtr<Reference>(reference, repo);
        }

        private int CreateSymbolicReference(string name, string target, bool allowOverwrite, out IntPtr reference)
        {
            return NativeMethods.git_reference_create_symbolic(out reference, repo.Handle, name, target, allowOverwrite);
        }

        private int CreateDirectReference(string name, ObjectId targetId, bool allowOverwrite, out IntPtr reference)
        {
            targetId = Unabbreviate(targetId);

            GitOid oid = targetId.Oid;

            return NativeMethods.git_reference_create_oid(out reference, repo.Handle, name, ref oid, allowOverwrite);
        }

        private ObjectId Unabbreviate(ObjectId targetId)
        {
            if (!(targetId is AbbreviatedObjectId))
            {
                return targetId;
            }

            GitObject obj = repo.Lookup(targetId);

            if (obj == null)
            {
                Ensure.Success((int)GitErrorCode.GIT_ENOTFOUND);
            }

            return obj.Id;
        }

        /// <summary>
        ///   Delete a reference with the specified name
        /// </summary>
        /// <param name = "name">The name of the reference to delete.</param>
        public void Delete(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference = RetrieveReferencePtr(name);

            int res = NativeMethods.git_reference_delete(reference);
            Ensure.Success(res);
        }

        /// <summary>
        ///   Rename an existing reference with a new name
        /// </summary>
        /// <param name = "currentName">The canonical name of the reference to rename.</param>
        /// <param name = "newName">The new canonical name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing reference, false otherwise.</param>
        /// <returns></returns>
        public Reference Move(string currentName, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "currentName");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            IntPtr referencePtr = RetrieveReferencePtr(currentName);

            int res = NativeMethods.git_reference_rename(referencePtr, newName, allowOverwrite);
            Ensure.Success(res);

            return Reference.BuildFromPtr<Reference>(referencePtr, repo);
        }

        internal T Resolve<T>(string name) where T : Reference
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference = RetrieveReferencePtr(name, false);

            return Reference.BuildFromPtr<T>(reference, repo);
        }

        /// <summary>
        ///   Updates the target on a reference.
        /// </summary>
        /// <param name = "name">The name of the reference.</param>
        /// <param name = "target">The target which can be either a sha or the name of another reference.</param>
        public Reference UpdateTarget(string name, string target)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            IntPtr reference = RetrieveReferencePtr(name);
            int res;

            ObjectId id;
            bool isObjectIdentifier = ObjectId.TryParse(target, out id);
            GitReferenceType type = NativeMethods.git_reference_type(reference);
            switch (type)
            {
                case GitReferenceType.Oid:
                    if (!isObjectIdentifier)
                    {
                        throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The reference specified by {0} is an Oid reference, you must provide a sha as the target.", name), "target");
                    }
                    GitOid oid = id.Oid;
                    res = NativeMethods.git_reference_set_oid(reference, ref oid);
                    break;

                case GitReferenceType.Symbolic:
                    if (isObjectIdentifier)
                    {
                        throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The reference specified by {0} is an Symbolic reference, you must provide a symbol as the target.", name), "target");
                    }
                    res = NativeMethods.git_reference_set_target(reference, target);
                    break;

                default:
                    throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture, "Reference '{0}' has an unexpected type ('{1}').", name, Enum.GetName(typeof(GitReferenceType), type)));
            }

            Ensure.Success(res);

            return Reference.BuildFromPtr<Reference>(reference, repo);
        }

        private IntPtr RetrieveReferencePtr(string referenceName, bool shouldThrow = true)
        {
            IntPtr reference;
            int res = NativeMethods.git_reference_lookup(out reference, repo.Handle, referenceName);

            if (!shouldThrow && res == (int)GitErrorCode.GIT_ENOTFOUND)
            {
                return IntPtr.Zero;
            }

            Ensure.Success(res);

            return reference;
        }

        internal GitObject RetrieveTargetObject(string target)
        {
            GitObject objectToTag = repo.Lookup(target);

            if (objectToTag == null)
            {
                throw new LibGit2Exception(String.Format(CultureInfo.InvariantCulture, "No object identified by '{0}' can be found in the repository.", target));
            }

            return objectToTag;
        }
    }
}
