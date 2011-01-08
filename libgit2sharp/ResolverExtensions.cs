namespace libgit2sharp
{
    public static class ResolverExtensions
    {
        public static GitObject Resolve(this IResolver resolver, string objectId)
        {
            return resolver.Resolve<GitObject>(objectId);
        }

        public static TType Resolve<TType>(this IResolver resolver, string objectId)
        {
            return (TType)resolver.Resolve(objectId, typeof(TType));
        }
    }
}