namespace LibGit2Sharp
{
    public interface IQueryableCommitCollection : ICommitCollection //TODO: Find a name that's more explicit than IQueryableCommitCollection
    {
        /// <summary>
        ///  Returns the list of commits of the repository matching the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A collection of commits, ready to be enumerated.</returns>
        ICommitCollection QueryBy(Filter filter);
    }
}