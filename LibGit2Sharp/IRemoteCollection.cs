namespace LibGit2Sharp
{
    public interface IRemoteCollection
    {
        Remote this[string name] { get; }
    }
}