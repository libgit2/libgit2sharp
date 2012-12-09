namespace LibGit2Sharp
{
    /// <summary>
    ///   Disambiguates the different versions of an index entry during a merge.
    /// </summary>
    public enum StageLevel
    {
        /// <summary>
        ///   The standard fully merged state for an index entry. 
        /// </summary>
        Staged = 0,

        /// <summary>
        ///   Version of the entry as it was in the common base merge commit.
        /// </summary>
        Ancestor = 1,
        
        /// <summary>
        ///   Version of the entry as it is in the commit of the Head.
        /// </summary>
        Ours = 2,

        /// <summary>
        ///   Version of the entry as it is in the commit being merged.
        /// </summary>
        Theirs = 3,
    }
}
