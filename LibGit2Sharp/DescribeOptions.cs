namespace LibGit2Sharp
{
    /// <summary>
    /// Options to define describe behaviour
    /// </summary>
    public sealed class DescribeOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescribeOptions"/> class.
        /// <para>
        ///   By default:
        ///    - Only annotated tags will be considered as reference points
        ///    - The commit id won't be used as a fallback strategy
        ///    - Only the 10 most recent tags will be considered as candidates to describe the commit
        ///    - All ancestor lines will be followed upon seeing a merge commit
        ///    - 7 hexacidemal digits will be used as a minimum commid abbreviated size
        ///    - Long format will only be used when no direct match has been found
        /// </para>
        /// </summary>
        public DescribeOptions()
        {
            Strategy = DescribeStrategy.Default;
            MinimumCommitIdAbbreviatedSize = 7;
            OnlyFollowFirstParent = false;
        }

        /// <summary>
        /// The kind of references that will be eligible as reference points.
        /// </summary>
        public DescribeStrategy Strategy { get; set; }

        /// <summary>
        /// Rather than throwing, should <see cref="IRepository.Describe"/> return
        /// the abbreviated commit id when the selected <see cref="Strategy"/>
        /// didn't identify a proper reference to describe the commit.
        /// </summary>
        public bool UseCommitIdAsFallback { get; set; }

        /// <summary>
        /// Number of minimum hexadecimal digits used to render a uniquely
        /// abbreviated commit id.
        /// </summary>
        public int MinimumCommitIdAbbreviatedSize { get; set; }

        /// <summary>
        /// Always output the long format (the tag, the number of commits
        /// and the abbreviated commit name) even when a direct match has been
        /// found.
        /// <para>
        ///   This is useful when one wants to see parts of the commit object
        ///   name in "describe" output, even when the commit in question happens
        ///   to be a tagged version. Instead of just emitting the tag name, it
        ///   will describe such a commit as v1.2-0-gdeadbee (0th commit since
        ///   tag v1.2 that points at object deadbee...).
        /// </para>
        /// </summary>
        public bool AlwaysRenderLongFormat { get; set; }

        /// <summary>
        /// Follow only the first parent commit upon seeing a merge commit.
        /// <para>
        ///   This is useful when you wish to not match tags on branches merged in
        ///   the history of the target commit.
        /// </para>
        /// </summary>
        public bool OnlyFollowFirstParent { get; set; }

        /// <summary>
        /// Only consider tags matching the given glob(7) pattern, excluding the "refs/tags/" prefix.
        /// </summary>
        public string Match { get; set; }
    }
}
