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

        /// <summary>
        ///   Creates a lightweight tag with the specified name. This tag will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being looked up.</param>
        /// <param name="tagName">The name of the tag to create.</param>
        public static Tag ApplyTag(this Repository repository, string tagName)
        {
            return repository.Tags.Create(tagName, repository.Head.CanonicalName);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name. This tag will point at the <paramref name="target"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being looked up.</param>
        /// <param name="tagName">The name of the tag to create.</param>
        /// <param name="target">The canonical reference name or sha which should be pointed at by the Tag.</param>
        public static Tag ApplyTag(this Repository repository, string tagName, string target)
        {
            return repository.Tags.Create(tagName, target);
        }
    }
}