using System;
using System.Collections;
using System.Collections.Generic;

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
        public const string HEAD = "HEAD";

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
            return GitReferenceHelper.List(this, repo.RepoPtr, GitReferenceType.ListAll).GetEnumerator();
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

            GitOid oid;
            if (NativeMethods.git_oid_mkstr(out oid, target) == (int) GitErrorCode.GIT_SUCCESS)
            {
                return Create(name, oid);
            }

            IntPtr reference;
            var res = NativeMethods.git_reference_create_symbolic(out reference, repo.RepoPtr, name, target);
            Ensure.Success(res);

            return Reference.CreateFromPtr(reference, repo);
        }

        /// <summary>
        ///   Creates a reference with the specified name and target
        /// </summary>
        /// <param name = "name">The name of the reference to create.</param>
        /// <param name = "target">The oid of the target.</param>
        /// <returns>A new <see cref = "Reference" />.</returns>
        public Reference Create(string name, GitOid target)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference;
            var res = NativeMethods.git_reference_create_oid(out reference, repo.RepoPtr, name, ref target);
            Ensure.Success(res);

            return Reference.CreateFromPtr(reference, repo);
        }

        /// <summary>
        ///   Shortcut to return the reference to HEAD
        /// </summary>
        /// <returns></returns>
        public Reference Head()
        {
            return this[HEAD];
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
            var res = NativeMethods.git_reference_lookup(out reference, repo.RepoPtr, name);
            Ensure.Success(res);

            return Reference.CreateFromPtr(reference, repo);
        }

        #region Nested type: GitReferenceHelper

        private static unsafe class GitReferenceHelper
        {
            public static List<Reference> List(ReferenceCollection owner, IntPtr repo, GitReferenceType types)
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