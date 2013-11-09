namespace LibGit2Sharp
{
    /// <summary>
    /// Optional adjustments to the behavior of blame.
    /// </summary>
    public class BlameOptions
    {
        /// <summary>
        /// Initialize to defaults
        /// </summary>
        public BlameOptions() { }

        /// <summary>
        /// Strategy to use to determine the blame for each line.
        /// The default is <see cref="BlameStrategy.Default"/>.
        /// </summary>
        public virtual BlameStrategy Strategy { get; set; }

        /// <summary>
        /// Latest commitish to consider (the starting point).
        /// If null, blame will use HEAD.
        /// </summary>
        public virtual string Until { get; set; }

        /// <summary>
        /// Oldest commitish to consider (the stopping point).
        /// If null, blame will continue until all the lines have been blamed,
        /// or until a commit with no parents is reached.
        /// </summary>
        public virtual string Since { get; set; }

        /// <summary>
        /// First text line in the file to blame (lines start at 1).
        /// The default is 1.
        /// </summary>
        public virtual int MinLine { get; set; }

        /// <summary>
        /// Last text line in the file to blame (lines start at 1).
        /// The default is the last line in the file.
        /// </summary>
        public virtual int MaxLine { get; set; }
    }
}