namespace LibGit2Sharp
{
    /// <summary>
    /// Parameters controlling Pull behavior.
    /// </summary>
    public sealed class PullOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PullOptions()
        { }

        /// <summary>
        /// Parameters controlling Fetch behavior.
        /// </summary>
        public FetchOptions FetchOptions { get; set; }

        /// <summary>
        /// Parameters controlling Merge behavior.
        /// </summary>
        public MergeOptions MergeOptions { get; set; }
    }
}
