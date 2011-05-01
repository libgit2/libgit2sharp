namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides helper overloads to a <see cref="Repository"/>.
    /// </summary>
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
    }
}