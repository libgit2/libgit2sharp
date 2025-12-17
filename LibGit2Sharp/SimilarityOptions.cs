namespace LibGit2Sharp
{
    /// <summary>
    /// Represents a mode for handling whitespace while detecting renames and copies.
    /// </summary>
    public enum WhitespaceMode
    {
        /// <summary>
        /// Don't consider leading whitespace when comparing files
        /// </summary>
        IgnoreLeadingWhitespace,

        /// <summary>
        /// Don't consider any whitespace when comparing files
        /// </summary>
        IgnoreAllWhitespace,

        /// <summary>
        /// Include all whitespace when comparing files
        /// </summary>
        DontIgnoreWhitespace,
    }

    /// <summary>
    /// Represents a mode for detecting renames and copies.
    /// </summary>
    public enum RenameDetectionMode
    {
        /// <summary>
        /// Obey the user's `diff.renames` configuration setting
        /// </summary>
        Default,

        /// <summary>
        /// Attempt no rename or copy detection
        /// </summary>
        None,

        /// <summary>
        /// Detect exact renames and copies (compare SHA hashes only)
        /// </summary>
        Exact,

        /// <summary>
        /// Detect fuzzy renames (use similarity metric)
        /// </summary>
        Renames,

        /// <summary>
        /// Detect renames and copies
        /// </summary>
        Copies,

        /// <summary>
        /// Detect renames, and include unmodified files when looking for copies
        /// </summary>
        CopiesHarder,
    }

    /// <summary>
    /// Options for handling file similarity
    /// </summary>
    public sealed class SimilarityOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimilarityOptions"/> class.
        /// </summary>
        public SimilarityOptions()
        {
            RenameDetectionMode = RenameDetectionMode.Default;
            WhitespaceMode = WhitespaceMode.IgnoreLeadingWhitespace;
            RenameThreshold = 50;
            RenameFromRewriteThreshold = 50;
            CopyThreshold = 50;
            BreakRewriteThreshold = 60;
            RenameLimit = 200;
        }

        /// <summary>
        /// Get a <see cref="SimilarityOptions"/> instance that does no rename detection
        /// </summary>
        public static SimilarityOptions None
        {
            get { return new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.None }; }
        }

        /// <summary>
        /// Get a <see cref="SimilarityOptions"/> instance that detects renames
        /// </summary>
        public static SimilarityOptions Renames
        {
            get { return new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.Renames }; }
        }

        /// <summary>
        /// Get a <see cref="SimilarityOptions"/> instance that detects exact renames only
        /// </summary>
        public static SimilarityOptions Exact
        {
            get { return new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.Exact }; }
        }

        /// <summary>
        /// Get a <see cref="SimilarityOptions"/> instance that detects renames and copies
        /// </summary>
        public static SimilarityOptions Copies
        {
            get { return new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.Copies }; }
        }

        /// <summary>
        /// Get a <see cref="SimilarityOptions"/> instance that detects renames, and includes unmodified files when detecting copies
        /// </summary>
        public static SimilarityOptions CopiesHarder
        {
            get { return new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.CopiesHarder }; }
        }

        /// <summary>
        /// Get a <see cref="SimilarityOptions"/> instance that obeys the user's `diff.renames` setting
        /// </summary>
        public static SimilarityOptions Default
        {
            get { return new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.Default }; }
        }

        /// <summary>
        /// The mode for detecting renames and copies
        /// </summary>
        public RenameDetectionMode RenameDetectionMode { get; set; }

        /// <summary>
        /// The mode for handling whitespace when comparing files
        /// </summary>
        public WhitespaceMode WhitespaceMode { get; set; }

        /// <summary>
        /// Similarity in order to consider a rename
        /// </summary>
        public int RenameThreshold { get; set; }

        /// <summary>
        /// Similarity of a modified file in order to be eligible as a rename source
        /// </summary>
        public int RenameFromRewriteThreshold { get; set; }

        /// <summary>
        /// Similarity to consider a file a copy
        /// </summary>
        public int CopyThreshold { get; set; }

        /// <summary>
        /// Similarity to split modify into an add/delete pair
        /// </summary>
        public int BreakRewriteThreshold { get; set; }

        /// <summary>
        /// Maximum similarity sources to examine for a file
        /// </summary>
        public int RenameLimit { get; set; }

        // TODO: custom metric
    }
}
