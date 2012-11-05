namespace LibGit2Sharp.Core
{
    internal interface ILazy<T>
    {
        T Value { get; }
    }
}
