namespace LibGit2Sharp
{
    /// <summary>
    /// Class to report the result of a revert.
    /// </summary>
    public class RevertResult
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RevertResult()
        { }

        internal RevertResult(RevertStatus status, Commit commit = null)
        {
            Commit = commit;
            Status = status;
        }

        /// <summary>
        /// The resulting commit of the revert.
        /// <para>
        ///   This will return <code>null</code> if the revert was not committed.
        ///     This can happen if:
        ///       1) The revert resulted in conflicts.
        ///       2) The option to not commit on success is set.
        ///   </para>
        /// </summary>
        public virtual Commit Commit { get; private set; }

        /// <summary>
        /// The status of the revert.
        /// </summary>
        public virtual RevertStatus Status { get; private set; }
    }

    /// <summary>
    /// The status of what happened as a result of a revert.
    /// </summary>
    public enum RevertStatus
    {
        /// <summary>
        /// The commit was successfully reverted.
        /// </summary>
        Reverted,

        /// <summary>
        /// The revert resulted in conflicts.
        /// </summary>
        Conflicts,

        /// <summary>
        /// Revert was run, but there were no changes to commit.
        /// </summary>
        NothingToRevert,
    }
}
