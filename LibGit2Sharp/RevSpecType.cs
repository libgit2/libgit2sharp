namespace LibGit2Sharp
{
    /// <summary>
    /// These indicate the intended behavior of the spec passed to git_revparse.
    /// </summary>
    public enum RevSpecType
    {
        /// <summary>
        /// The spec targeted a single object. 
        /// </summary>
        Single = 1 << 0,

        /// <summary>
        /// The spec targeted a range of commits. 
        /// </summary>
        Range = 1 << 1,

        /// <summary>
        /// The spec used the '...' operator, which invokes special semantics. 
        /// </summary>
        MergeBase = 1 << 2,
    }
}