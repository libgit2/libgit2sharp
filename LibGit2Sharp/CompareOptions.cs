namespace LibGit2Sharp
{
    /// <summary>
    /// Options to define file comparison behavior.
    /// </summary>
    public sealed class CompareOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompareOptions"/> class.
        /// </summary>
        public CompareOptions()
        {
            ContextLines = 3;
            InterhunkLines = 0;
            SkipPatchBuilding = false;
        }

        /// <summary>
        /// The number of unchanged lines that define the boundary of a hunk (and to display before and after).
        /// (Default = 3)
        /// </summary>
        public int ContextLines { get; set; }

        /// <summary>
        /// The maximum number of unchanged lines between hunk boundaries before the hunks will be merged into a one.
        /// (Default = 0)
        /// </summary>
        public int InterhunkLines { get; set; }

        /// <summary>
        /// Flag to skip patch building. May be used if only file name and status required.
        /// (Default = false)
        /// </summary>
        internal bool SkipPatchBuilding { get; set; }
    }
}
