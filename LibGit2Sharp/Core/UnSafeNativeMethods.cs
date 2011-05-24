using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal static unsafe class UnSafeNativeMethods
    {
        private const string libgit2 = "git2-0.dll";

        [DllImport(libgit2)]
        public static extern int git_reference_listall(git_strarray* array, RepositorySafeHandle repo, GitReferenceType flags);

        [DllImport(libgit2)]
        public static extern int git_tag_list(git_strarray* array, RepositorySafeHandle repo);

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
}