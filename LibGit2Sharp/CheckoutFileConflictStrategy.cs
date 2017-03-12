namespace LibGit2Sharp
{
    /// <summary>
    /// Enum specifying what content checkout should write to disk
    /// for conflicts.
    /// </summary>
    public enum CheckoutFileConflictStrategy
    {
        /// <summary>
        /// Use the default behavior for handling file conflicts. This is
        /// controlled by the merge.conflictstyle config option, and is "Merge"
        /// if no option is explicitly set.
        /// </summary>
        Normal,

        /// <summary>
        /// For conflicting files, checkout the "ours" (stage 2)  version of
        /// the file from the index.
        /// </summary>
        Ours,

        /// <summary>
        /// For conflicting files, checkout the "theirs" (stage 3) version of
        /// the file from the index.
        /// </summary>
        Theirs,

        /// <summary>
        /// Write normal merge files for conflicts.
        /// </summary>
        Merge,

        /// <summary>
        /// Write diff3 formated files for conflicts.
        /// </summary>
        Diff3
    }
}
