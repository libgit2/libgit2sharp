namespace LibGit2Sharp
{
    /// <summary>
    /// Submodule update rule options.
    /// </summary>
    public enum SubmoduleUpdate
    {
        /// <summary>
        /// Reset to the last saved update rule.
        /// </summary>
        Reset = -1,
        /// <summary>
        /// Only used when you don't want to specify any particular update
        /// rule.
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// This is the default - checkout the commit recorded in the superproject.
        /// </summary>
        Checkout = 1,
        /// <summary>
        /// Rebase the current branch of the submodule onto the commit recorded in the superproject.
        /// </summary>
        Rebase = 2,
        /// <summary>
        /// Merge the commit recorded in the superproject into the current branch of the submodule.
        /// </summary>
        Merge = 3,
        /// <summary>
        /// Do not update the submodule.
        /// </summary>
        None = 4,
    }
}
