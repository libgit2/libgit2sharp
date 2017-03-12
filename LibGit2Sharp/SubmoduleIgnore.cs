namespace LibGit2Sharp
{
    /// <summary>
    /// Values that could be specified for how closely to examine the
    /// working directory when getting submodule status.
    /// </summary>
    public enum SubmoduleIgnore
    {
        /// <summary>
        /// Reset to the last saved ignore rule.
        /// </summary>
        Reset = -1,

        /// <summary>
        /// Any change or untracked == dirty
        /// </summary>
        None = 1,

        /// <summary>
        /// Dirty if tracked files change
        /// </summary>
        Untracked = 2,

        /// <summary>
        /// Only dirty if HEAD moved
        /// </summary>
        Dirty = 3,

        /// <summary>
        /// Never dirty
        /// </summary>
        All = 4,
    }
}
