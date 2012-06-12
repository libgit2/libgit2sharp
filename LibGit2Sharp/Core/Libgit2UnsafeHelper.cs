using System;
using System.Collections.Generic;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal static unsafe class Libgit2UnsafeHelper
    {
        public static IList<string> ListAllBranchNames(RepositorySafeHandle repo, GitBranchType types)
        {
            UnSafeNativeMethods.git_strarray strArray;
            int res = UnSafeNativeMethods.git_branch_list(out strArray, repo, types);
            Ensure.Success(res);

            return BuildListOf(strArray);
        }

        public static IList<string> ListAllReferenceNames(RepositorySafeHandle repo, GitReferenceType types)
        {
            UnSafeNativeMethods.git_strarray strArray;
            int res = UnSafeNativeMethods.git_reference_list(out strArray, repo, types);
            Ensure.Success(res);

            return BuildListOf(strArray);
        }

        public static IList<string> ListAllRemoteNames(RepositorySafeHandle repo)
        {
            UnSafeNativeMethods.git_strarray strArray;
            int res = UnSafeNativeMethods.git_remote_list(out strArray, repo);
            Ensure.Success(res);

            return BuildListOf(strArray);
        }

        public static IList<string> ListAllTagNames(RepositorySafeHandle repo)
        {
            UnSafeNativeMethods.git_strarray strArray;
            int res = UnSafeNativeMethods.git_tag_list(out strArray, repo);
            Ensure.Success(res);

            return BuildListOf(strArray);
        }

        private static IList<string> BuildListOf(UnSafeNativeMethods.git_strarray strArray)
        {
            var list = new List<string>();

            try
            {
                UnSafeNativeMethods.git_strarray* gitStrArray = &strArray;

                uint numberOfEntries = gitStrArray->size;
                for (uint i = 0; i < numberOfEntries; i++)
                {
                    var name = Utf8Marshaler.FromNative((IntPtr)gitStrArray->strings[i]);
                    list.Add(name);
                }

                list.Sort(StringComparer.Ordinal);
            }
            finally
            {
                UnSafeNativeMethods.git_strarray_free(ref strArray);
            }

            return list;
        }
    }
}
