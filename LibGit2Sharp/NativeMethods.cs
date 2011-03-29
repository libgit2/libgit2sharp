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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    internal unsafe class UnSafeNativeMethods
    {
        private const string libgit2 = "git2.dll";

        [DllImport(libgit2)]
        public static extern int git_reference_listall(git_strarray* array, IntPtr repo, GitReferenceType flags);

        [DllImport(libgit2)]
        public static extern void git_strarray_free(git_strarray* array);

        #region Nested type: git_strarray

        internal struct git_strarray
        {
            public sbyte** strings;
            public IntPtr size;
        }

        #endregion
    }

    internal class NativeMethods
    {
        private const string libgit2 = "git2.dll";

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_reference_delete(IntPtr reference);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_reference_create_oid(out IntPtr reference, IntPtr repo, string name, ref GitOid oid);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_reference_create_symbolic(out IntPtr reference, IntPtr repo, string name, string target);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_commit_author(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_commit_committer(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_commit_create_o(out GitOid oid, IntPtr repo, string updateRef, IntPtr author, IntPtr committer, string message, IntPtr tree, int parentCount, IntPtr parents);

        [DllImport(libgit2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_commit_message(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_commit_message_short(IntPtr commit);

        [DllImport(libgit2)]
        public static extern int git_commit_parent(out IntPtr parentCommit, IntPtr commit, uint n);

        [DllImport(libgit2)]
        public static extern uint git_commit_parentcount(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_commit_tree(out IntPtr tree, IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_object_close(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_object_id(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_object_lookup(out IntPtr obj, IntPtr repo, ref GitOid id, GitObjectType type);

        [DllImport(libgit2, SetLastError = true)]
        public static extern GitObjectType git_object_type(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern bool git_odb_exists(IntPtr db, ref GitOid id);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_odb_object_close(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_oid_cmp(ref GitOid a, ref GitOid b);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_oid_fmt(byte[] str, ref GitOid oid);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_oid_mkstr(out GitOid oid, string str);

        [DllImport(libgit2)]
        public static extern int git_reference_lookup(out IntPtr reference, IntPtr repo, string name);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_reference_name(IntPtr reference);

        [DllImport(libgit2)]
        public static extern IntPtr git_reference_oid(IntPtr reference);

        [DllImport(libgit2)]
        public static extern int git_reference_resolve(out IntPtr resolvedReference, IntPtr reference);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_reference_target(IntPtr reference);

        [DllImport(libgit2)]
        public static extern GitReferenceType git_reference_type(IntPtr reference);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_repository_database(IntPtr repository);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_repository_free(IntPtr repository);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_repository_init(out IntPtr repository, string path, bool isBare);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_repository_open(out IntPtr repository, string path);

        [DllImport(libgit2)]
        public static extern void git_revwalk_free(IntPtr walker);

        [DllImport(libgit2)]
        public static extern int git_revwalk_new(out IntPtr walker, IntPtr repo);

        [DllImport(libgit2)]
        public static extern int git_revwalk_next(out GitOid oid, IntPtr walker);

        [DllImport(libgit2)]
        public static extern int git_revwalk_push(IntPtr walker, ref GitOid oid);

        [DllImport(libgit2)]
        public static extern void git_revwalk_reset(IntPtr walker);

        [DllImport(libgit2)]
        public static extern void git_revwalk_sorting(IntPtr walk, GitSortOptions sort);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_signature_free(IntPtr signature);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_signature_new(string name, string email, long time, int offset);
    }
}