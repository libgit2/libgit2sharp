namespace LibGit2Sharp
{
    /// <summary>
    ///   Specifies the kind of operation that <see cref="Repository.Reset(LibGit2Sharp.ResetOptions, string)"/> should perform.
    /// </summary>
    public enum ResetOptions
    {
        /// <summary>
        ///   Moves the branch pointed to by HEAD to the specified commit object.
        /// </summary>
        Soft,

        /// <summary>
        ///   Moves the branch pointed to by HEAD to the specified commit object and resets the index
        ///   to the tree recorded by the commit.
        /// </summary>
        Mixed,
    }
}
