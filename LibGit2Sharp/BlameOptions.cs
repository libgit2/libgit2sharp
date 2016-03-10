namespace LibGit2Sharp
{
    /// <summary>
    /// Strategy used for blaming.
    /// </summary>
    public enum BlameStrategy
    {
        /// <summary>
        /// Track renames of the file using diff rename detection, but no block movement.
        /// </summary>
        Default,

        // Track copies within the same file. (NOT SUPPORTED IN LIBGIT2 YET)
        //TrackCopiesSameFile,

        // Track movement across files within the same commit. (NOT SUPPORTED IN LIBGIT2 YET)
        //TrackCopiesSameCommitMoves,

        // Track copies across files within the same commit. (NOT SUPPORTED IN LIBGIT2 YET)
        //TrackCopiesSameCommitCopies,

        // Track copies across all files in all commits. (NOT SUPPORTED IN LIBGIT2 YET)
        //TrackCopiesAnyCommitCopies
    }

    /// <summary>
    /// Optional adjustments to the behavior of blame.
    /// </summary>
    public sealed class BlameOptions
    {
        /// <summary>
        /// Strategy to use to determine the blame for each line.
        /// The default is <see cref="BlameStrategy.Default"/>.
        /// </summary>
        public BlameStrategy Strategy { get; set; }

        /// <summary>
        /// Latest commitish to consider (the starting point).
        /// If null, blame will use HEAD.
        /// </summary>
        public object StartingAt { get; set; }

        /// <summary>
        /// Oldest commitish to consider (the stopping point).
        /// If null, blame will continue until all the lines have been blamed,
        /// or until a commit with no parents is reached.
        /// </summary>
        public object StoppingAt { get; set; }

        /// <summary>
        /// First text line in the file to blame (lines start at 1).
        /// If this is set to 0, the blame begins at line 1.
        /// </summary>
        public int MinLine { get; set; }

        /// <summary>
        /// Last text line in the file to blame (lines start at 1).
        /// If this is set to 0, blame ends with the last line in the file.
        /// </summary>
        public int MaxLine { get; set; }

        /// <summary>
        /// Disables rename heuristics, only matching renames on unmodified files.
        /// </summary>
        public bool FindExactRenames { get; set; }

        /// <summary>
        /// Fully disable rename checking.
        /// </summary>
        public bool FindNoRenames { get; set; }
    }
}
