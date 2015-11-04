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
        Myers = 0,

        /// <summary>
        /// Use "minimal diff" algorithm when generating patches.
        /// </summary>
        Minimal = 1,

        /// <summary>
        /// Use "patience diff" algorithm when generating patches.
        /// </summary>
        Patience = 2,
    }
}
