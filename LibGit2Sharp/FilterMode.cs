namespace LibGit2Sharp
{
    /// <summary>
    /// These values control which direction of change is with which which a filter is being applied.
    /// </summary>
    public enum FilterMode
    {
        /// <summary>
        /// Smudge - occurs when exporting a file from the Git object database to the working directory,
        /// </summary>
        Smudge = 0,

        /// <summary>
        /// Clean - occurs when importing a file from the working directory to the Git object database.
        /// </summary>
        Clean = (1 << 0),
    }
}