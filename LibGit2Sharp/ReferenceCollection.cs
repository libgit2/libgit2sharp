#region  Copyright (c) 2011 LibGit2Sharp committers

//  The MIT License
//  
//  Copyright (c) 2011 LibGit2Sharp committers
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    public class ReferenceCollection : IEnumerable<Reference>
    {
        public const string HEAD = "HEAD";

        private readonly Repository repo;

        public ReferenceCollection(Repository repo)
        {
            this.repo = repo;
        }

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

        public Reference Create(string name, GitOid oid)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            IntPtr reference;
            var res = NativeMethods.git_reference_create_oid(out reference, repo.RepoPtr, name, ref oid);
            Ensure.Success(res);

            return Reference.CreateFromPtr(reference, repo);
        }

        public Reference Head()
        {
            return this[HEAD];
        }

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