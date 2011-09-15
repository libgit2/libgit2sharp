using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
    internal static unsafe class Libgit2UnsafeHelper
    {
        public static IList<string> ListAllReferenceNames(RepositorySafeHandle repo, GitReferenceType types)
        {
            UnSafeNativeMethods.git_strarray strArray;
            int res = UnSafeNativeMethods.git_reference_listall(out strArray, repo, types);
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
                    var name = new string(gitStrArray->strings[i]);
                    list.Add(name);
                }
            }
            finally
            {
                UnSafeNativeMethods.git_strarray_free(ref strArray);
            }

            return list;
        }
    }
}
