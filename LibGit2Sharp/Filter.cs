namespace LibGit2Sharp
{
    /// <summary>
    /// Options used to filter out the commits of the repository when querying its history.
    /// </summary>
    public class Filter
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Filter"/>.
        /// </summary>
        public Filter()
        {
            SortBy = GitSortOptions.Time;
            Since = "HEAD";
        }

        /// <summary>
        /// The ordering stragtegy to use.
        /// <para>
        /// By default, the commits are shown in reverse chronological order.
        /// </para>
        /// </summary>
        public GitSortOptions SortBy { get; set; }

        /// <summary>
        /// The pointer to the commit to consider as a starting point.
        /// <para>
        /// Can be either a <see cref="string"/> containing the sha or reference canonical name to use, a <see cref="Branch"/> or a <see cref="Reference"/>.
        /// By default, the <see cref="Repository.Head"/> will be used as boundary.
        /// </para>
        /// </summary>
        public object Since { get; set; }


        /// <summary>
        /// The pointer to the commit which will be excluded (along with its ancestors) from the enumeration.
        /// <para>
        /// Can be either a <see cref="string"/> containing the sha or reference canonical name to use, a <see cref="Branch"/> or a <see cref="Reference"/>.
        /// </para>
        /// </summary>
        public object Until { get; set; }
    }
}