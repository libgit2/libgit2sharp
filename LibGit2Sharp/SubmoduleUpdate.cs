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
        Default = -1,
        /// <summary>
        /// Checkout the commit recorded in the superproject.
        /// </summary>
        Checkout = 0,
        /// <summary>
        /// Rebase the current branch of the submodule onto the commit recorded in the superproject.
        /// </summary>
        Rebase = 1,
        /// <summary>
        /// Merge the commit recorded in the superproject into the current branch of the submodule.
        /// </summary>
        Merge = 2,
        /// <summary>
        /// Do not update the submodule.
        /// </summary>
        None = 3,
    }
}
