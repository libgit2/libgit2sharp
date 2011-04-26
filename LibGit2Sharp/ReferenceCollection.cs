using System;
using System.Collections;
using System.Collections.Generic;
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
        public ReferenceCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Reference" /> with the specified name.
        /// </summary>
        public Reference this[string name]
        {
            get { return Resolve<Reference>(name); }
        }

        #region IEnumerable<Reference> Members

        public IEnumerator<Reference> GetEnumerator()
        {
            return Libgit2UnsafeHelper
                .ListAllReferenceNames(repo.Handle, GitReferenceType.ListAll)
                .Select(n => this[n])
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Creates a reference with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "target">The target which can be either a sha or the name of another reference.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public Reference Create(string name, string target)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            ObjectId id = ObjectId.CreateFromMaybeSha(target);

            IntPtr reference;
            int res;

            if (id != null)
            {
                var oid = id.Oid;
                res = NativeMethods.git_reference_create_oid(out reference, repo.Handle, name, ref oid);
            }
            else
            {
                res = NativeMethods.git_reference_create_symbolic(out reference, repo.Handle, name, target);
            }

            Ensure.Success(res);

            return Reference.BuildFromPtr<Reference>(reference, repo);
        }

        /// <summary>
        ///   Delete a reference with the specified name
        /// </summary>
        public void Delete(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference = RetrieveReferencePtr(name);

            int res = NativeMethods.git_reference_delete(reference);
            Ensure.Success(res);
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Reference" /> with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <returns></returns>
        internal T Resolve<T>(string name) where T : class
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference = RetrieveReferencePtr(name);

            return Reference.BuildFromPtr<T>(reference, repo);
        }

        /// <summary>
        ///   Updates the target on a reference.
        /// </summary>
        /// <param name = "name">The name of the reference.</param>
        /// <param name = "target">The target which can be either a sha or the name of another reference.</param>
        public void UpdateTarget(string name, string target)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            IntPtr reference = RetrieveReferencePtr(name);
            int res;

            var id = ObjectId.CreateFromMaybeSha(target);
            var type = NativeMethods.git_reference_type(reference);
            switch (type)
            {
                case GitReferenceType.Oid:
                    if (id == null) throw new ArgumentException(String.Format("The reference specified by {0} is an Oid reference, you must provide a sha as the target.", name), "target");
                    var oid = id.Oid;
                    res = NativeMethods.git_reference_set_oid(reference, ref oid);
                    break;
                case GitReferenceType.Symbolic:
                    if (id != null) throw new ArgumentException(String.Format("The reference specified by {0} is an Symbolic reference, you must provide a symbol as the target.", name), "target");
                    res = NativeMethods.git_reference_set_target(reference, target);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Reference '{0}' has an un unexpected type ('{1}').", name, Enum.GetName(typeof(GitReferenceType), type)));
            }

            Ensure.Success(res);
        }

        private IntPtr RetrieveReferencePtr(string referenceName)
        {
            IntPtr reference;
            var res = NativeMethods.git_reference_lookup(out reference, repo.Handle, referenceName);
            Ensure.Success(res);

            return reference;
        }
    }
}