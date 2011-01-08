namespace libgit2sharp
{
    public interface IRefsResolver
    {
        Ref Resolve(string referenceName);
    }
}