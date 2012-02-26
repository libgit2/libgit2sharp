using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal static unsafe class UnSafeNativeMethods
    {
        private const string libgit2 = "git2";

        [DllImport(libgit2)]
        public static extern int git_reference_listall(out git_strarray array, RepositorySafeHandle repo, GitReferenceType flags);

        [DllImport(libgit2)]
        public static extern int git_remote_list(out git_strarray array, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_tag_list(out git_strarray array, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern void git_strarray_free(ref git_strarray array);

        #region Nested type: git_strarray

        internal struct git_strarray
        {
            public sbyte** strings;
            public IntPtr size;
        }

        #endregion
    }
}
