using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal static unsafe class Libgit2UnsafeHelper
    {
        public static IList<string> ListAllReferenceNames(RepositorySafeHandle repo, GitReferenceType types)
        {
            UnSafeNativeMethods.git_strarray strArray;
            var res = UnSafeNativeMethods.git_reference_listall(&strArray, repo, types);
            Ensure.Success(res);

            return BuildListOf(&strArray);
        }

        public static IList<string> ListAllTagNames(RepositorySafeHandle repo)
        {
            UnSafeNativeMethods.git_strarray strArray;
            var res = UnSafeNativeMethods.git_tag_list(&strArray, repo);
            Ensure.Success(res);

            return BuildListOf(&strArray);
        }

        private static IList<string> BuildListOf(UnSafeNativeMethods.git_strarray* strArray)
        {
            var list = new List<string>();

            try
            {
                int numberOfEntries = strArray->size.ToInt32();
                for (uint i = 0; i < numberOfEntries; i++)
                {
                    var name = new string(strArray->strings[i]);
                    list.Add(name);
                }
            }
            finally
            {
                UnSafeNativeMethods.git_strarray_free(strArray);
            }

            return list;
        }
    }
}