namespace LibGit2Sharp
{
    /// <summary>
    /// Options to define file staging behavior.
    /// </summary>
    public sealed class StageOptions
    {
        /// <summary>
        /// Stage ignored files. (Default = false)
        /// </summary>
        public bool IncludeIgnored { get; set; }

        /// <summary>
        /// If set, the passed paths will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths
        /// should be handled. (Default = null)
        /// </summary>
        public ExplicitPathsOptions ExplicitPathsOptions { get; set; }
    }
}
