namespace LibGit2Sharp
{
    public interface IQueryableCommitCollection : ICommitCollection //TODO: Find a name that's more explicit than IQueryableCommitCollection
    {
        /// <summary>
        ///   Returns the list of commits of the repository matching the specified <paramref name = "filter" />.
        /// </summary>
        /// <param name = "filter">The options used to control which commits will be returned.</param>
        /// <returns>A collection of commits, ready to be enumerated.</returns>
        ICommitCollection QueryBy(Filter filter);

        /// <summary>
        ///   Stores the content of the content as a new <see cref = "Commit" /> into the repository.
        /// </summary>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <returns>The generated <see cref = "Commit" />.</returns>
        Commit Create(Signature author, Signature committer, string message);
    }
}
