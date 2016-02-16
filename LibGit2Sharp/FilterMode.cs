namespace LibGit2Sharp
{
    /// <summary>
    /// These values control which direction of change is with which which a filter is being applied.
    /// </summary>
    /// <remarks>
    /// These enum values must be identical to the values in Libgit2 filter_mode_t found in filter.h
    /// </remarks>
    public enum FilterMode
    {
        /// <summary>
        /// Filter is being applied to content being moved into the working tree.
        /// </summary>
        ToWorkingTree = 0,
        /// <summary>
        /// Smudge occurs when exporting a file from the Git object database to the working directory.
        /// For example, a file would be smudged during a checkout operation.
        /// </summary>
        Smudge = ToWorkingTree,

        /// <summary>
        /// Filter is being applied to content being moved into the object database.
        /// </summary>
        ToOdb = 1,
        /// <summary>
        /// Clean occurs when importing a file from the working directory to the Git object database.
        /// For example, a file would be cleaned when staging a file.
        /// </summary>
        Clean = ToOdb,
    }
}
