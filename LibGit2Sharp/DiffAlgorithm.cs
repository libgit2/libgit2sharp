namespace LibGit2Sharp
{
    /// <summary>
    /// Algorithm used when performing a Diff.
    /// </summary>
    public enum DiffAlgorithm
    {
        /// <summary>
        /// The basic greedy diff algorithm.
        /// </summary>
        Meyers = 0,

        /// <summary>
        /// Use "patience diff" algorithm when generating patches.
        /// </summary>
        Patience = 2,
    }
}
