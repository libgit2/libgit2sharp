using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public static class RepositoryExtensions
    {
        /// <summary>
        ///   Try to lookup an object by its sha or a reference name.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name="repository">The <see cref="Repository"/> being looked up.</param>
        /// <param name = "shaOrRef">The shaOrRef to lookup.</param>
        /// <returns></returns>
        public static T Lookup<T>(this Repository repository, string shaOrRef) where T : GitObject
        {
            return (T)repository.Lookup(shaOrRef, GitObject.TypeToTypeMap[typeof(T)]);
        }

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" />.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name="repository">The <see cref="Repository"/> being looked up.</param>
        /// <param name = "id">The id.</param>
        /// <returns></returns>
        public static T Lookup<T>(this Repository repository, ObjectId id) where T : GitObject
        {
            return (T)repository.Lookup(id, GitObject.TypeToTypeMap[typeof(T)]);
        }

        /// <summary>
        ///   Try to lookup an object by its sha or a reference name and <see cref="GitObjectType"/>. If no matching object is found, null will be returned.
        /// 
        ///   Exceptions:
        ///   ArgumentNullException
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being looked up.</param>
        /// <param name = "shaOrRef">The shaOrRef to lookup.</param>
        /// <param name = "type"></param>
        /// <returns>the <see cref = "GitObject" /> or null if it was not found.</returns>
        public static GitObject Lookup(this Repository repository, string shaOrRef, GitObjectType type = GitObjectType.Any)
        {
            ObjectId id = ObjectId.CreateFromMaybeSha(shaOrRef);
            if (id != null)
            {
                return repository.Lookup(id, type);
            }

            var reference = repository.Refs[shaOrRef];
            return repository.Lookup(reference.ResolveToDirectReference().Target.Id, type);
        }

    }
}