using System;
using System.Collections.Generic;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal static unsafe class Libgit2UnsafeHelper
    {
        private static readonly Utf8Marshaler marshaler = (Utf8Marshaler)Utf8Marshaler.GetInstance(string.Empty);

        public static IList<string> ListAllReferenceNames(RepositorySafeHandle repo, GitReferenceType types)
        {
            UnSafeNativeMethods.git_strarray strArray;
            int res = UnSafeNativeMethods.git_reference_listall(out strArray, repo, types);
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

                int numberOfEntries = gitStrArray->size.ToInt32();
                for (uint i = 0; i < numberOfEntries; i++)
                {
                    var name = (string)marshaler.MarshalNativeToManaged((IntPtr)gitStrArray->strings[i]);
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
