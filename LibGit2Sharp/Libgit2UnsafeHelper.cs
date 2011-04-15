using System;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal static unsafe class Libgit2UnsafeHelper
    {
        public static IList<Reference> ListAllRefs(ReferenceCollection owner, RepositorySafeHandle repo, GitReferenceType types)
        {
            UnSafeNativeMethods.git_strarray strArray;
            var res = UnSafeNativeMethods.git_reference_listall(&strArray, repo, types);
            Ensure.Success(res);

            return BuildListOf(&strArray, name => owner[name]);
        }

        public static IList<Tag> ListAllTags(TagCollection owner, RepositorySafeHandle repo)
        {
            UnSafeNativeMethods.git_strarray strArray;
            var res = UnSafeNativeMethods.git_tag_list(&strArray, repo);
            Ensure.Success(res);

            return BuildListOf(&strArray, name => owner[name]);
        }

        private static IList<T> BuildListOf<T>(UnSafeNativeMethods.git_strarray* strArray, Func<string, T> instanceBuilder)
        {
            var list = new List<T>();

            try
            {
                for (uint i = 0; i < strArray->size.ToInt32(); i++)
                {
                    var name = new string(strArray->strings[i]);
                    list.Add(instanceBuilder(name));
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