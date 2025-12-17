using System;

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
            Algorithm = DiffAlgorithm.Myers;
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
        /// Options for rename detection. If null, the `diff.renames` configuration setting is used.
        /// </summary>
        public SimilarityOptions Similarity { get; set; }

        /// <summary>
        /// Include "unmodified" entries in the results.
        /// </summary>
        public bool IncludeUnmodified { get; set; }

        /// <summary>
        /// Algorithm to be used when performing a Diff.
        /// By default, <see cref="DiffAlgorithm.Myers"/> will be used.
        /// </summary>
        public DiffAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Enable --indent-heuristic Diff option, that attempts to produce more aesthetically pleasing diffs.
        /// By default, this option will be false.
        /// </summary>
        public bool IndentHeuristic { get; set; }
    }
}
