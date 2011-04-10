using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The Collection of references in a <see cref = "Repository" />
    /// </summary>
    public class ReferenceCollection : IEnumerable<Reference>
    {
        /// <summary>
        ///   A special Reference name to refer to the 'HEAD'
        /// </summary>
        private const string headReferenceName = "HEAD";

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
            get { return Resolve(name); }
        }

        #region IEnumerable<Reference> Members

        public IEnumerator<Reference> GetEnumerator()
        {
            return GitReferenceHelper.List(this, repo.Handle, GitReferenceType.ListAll).GetEnumerator();
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
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            ObjectId id = ObjectId.CreateFromMaybeSha(target);
            if (id != null)
            {
                return Create(name, id);
            }

            IntPtr reference;
            var res = NativeMethods.git_reference_create_symbolic(out reference, repo.Handle, name, target);
            Ensure.Success(res);

            return Reference.CreateFromPtr(reference, repo);
        }

        /// <summary>
        ///   Creates a reference with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "target">The oid of the target.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public Reference Create(string name, ObjectId target)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            var oid = target.Oid;
            IntPtr reference;
            var res = NativeMethods.git_reference_create_oid(out reference, repo.Handle, name, ref oid);
            Ensure.Success(res);

            return Reference.CreateFromPtr(reference, repo);
        }

        /// <summary>
        /// Delete a reference with the specified name
        /// </summary>
        public void Delete(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference;
            var res = NativeMethods.git_reference_lookup(out reference, repo.Handle, name);
            Ensure.Success(res);
            res = NativeMethods.git_reference_delete(reference);
            Ensure.Success(res);
        }

        /// <summary>
        ///   Shortcut to return the reference to HEAD
        /// </summary>
        /// <returns></returns>
        public Reference Head
        {
            get { return this[headReferenceName]; }
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Reference" /> with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <returns></returns>
        public Reference Resolve(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference;
            var res = NativeMethods.git_reference_lookup(out reference, repo.Handle, name);
            Ensure.Success(res);

            return Reference.CreateFromPtr(reference, repo);
        }

        #region Nested type: GitReferenceHelper

        private static unsafe class GitReferenceHelper
        {
            public static List<Reference> List(ReferenceCollection owner, RepositorySafeHandle repo, GitReferenceType types)
            {
                UnSafeNativeMethods.git_strarray strArray;
                var res = UnSafeNativeMethods.git_reference_listall(&strArray, repo, types);
                Ensure.Success(res);

                var list = new List<Reference>();

                try
                {
                    for (uint i = 0; i < strArray.size.ToInt32(); i++)
                    {
                        var name = new string(strArray.strings[i]);
                        list.Add(owner.Resolve(name));
                    }
                }
                finally
                {
                    UnSafeNativeMethods.git_strarray_free(&strArray);
                }

                return list;
            }
        }

        #endregion
    }
}