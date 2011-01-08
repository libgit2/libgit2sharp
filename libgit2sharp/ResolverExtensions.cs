namespace libgit2sharp
{
    public static class ResolverExtensions
    {
        public static GitObject Resolve(this IObjectResolver objectResolver, string objectId)
        {
            return objectResolver.Resolve<GitObject>(objectId);
        }

        public static TType Resolve<TType>(this IObjectResolver objectResolver, string objectId)
        {
            return (TType)objectResolver.Resolve(objectId, typeof(TType));
        }
    }
}