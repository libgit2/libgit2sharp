namespace LibGit2Sharp
{
    /// <summary>
    /// Submodule recurse rule options.
    /// </summary>
    public enum SubmoduleRecurse
    {
        /// <summary>
        /// Reset to the value in the config.
        /// </summary>
        Reset = -1,
        /// <summary>
        /// Do not recurse into submodules.
        /// </summary>
        No = 0,
        /// <summary>
        /// Recurse into submodules.
        /// </summary>
        Yes = 1,
        /// <summary>
        /// Recurse into submodules only when commit not already in local clone.
        /// </summary>
        OnDemand = 2,
    }
}
