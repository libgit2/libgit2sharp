namespace LibGit2Sharp
{
    /// <summary>
    /// The current progress of the stash application.
    /// </summary>
    public enum StashApplyProgress
    {
        /// <summary>
        /// Not passed by the callback. Used as dummy value.
        /// </summary>
        None = 0,

        /// <summary>
        /// Loading the stashed data from the object database.
        /// </summary>
        LoadingStash,

        /// <summary>
        /// The stored index is being analyzed.
        /// </summary>
        AnalyzeIndex,

        /// <summary>
        /// The modified files are being analyzed.
        /// </summary>
        AnalyzeModified,

        /// <summary>
        /// The untracked and ignored files are being analyzed.
        /// </summary>
        AnalyzeUntracked,

        /// <summary>
        /// The untracked files are being written to disk.
        /// </summary>
        CheckoutUntracked,

        /// <summary>
        /// The modified files are being written to disk.
        /// </summary>
        CheckoutModified,

        /// <summary>
        /// The stash was applied successfully.
        /// </summary>
        Done,
    }
}
