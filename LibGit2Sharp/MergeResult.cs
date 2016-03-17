namespace LibGit2Sharp
{
    /// <summary>
    /// Class to report the result of a merge.
    /// </summary>
    public class MergeResult
    {
        /// <summary>
        /// The status of the merge.
        /// </summary>
        public virtual MergeStatus Status { get; set; }

        /// <summary>
        /// The resulting commit of the merge. For fast-forward merges, this is the
        /// commit that merge was fast forwarded to.
        /// <para>This will return <code>null</code> if the merge has been unsuccessful due to conflicts.</para>
        /// </summary>
        public virtual Commit Commit { get; set; }
    }

    /// <summary>
    /// The status of what happened as a result of a merge.
    /// </summary>
    public enum MergeStatus
    {
        /// <summary>
        /// Merge was up-to-date.
        /// </summary>
        UpToDate,

        /// <summary>
        /// Fast-forward merge.
        /// </summary>
        FastForward,

        /// <summary>
        /// Non-fast-forward merge.
        /// </summary>
        NonFastForward,

        /// <summary>
        /// Merge resulted in conflicts.
        /// </summary>
        Conflicts,
    }
}
