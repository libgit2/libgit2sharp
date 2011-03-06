namespace LibGit2Sharp
{
    public interface IRefsResolver
    {
        Ref Resolve(string referenceName);
    }
}