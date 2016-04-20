namespace LibGit2Sharp
{
    /// <summary>
    /// Class to report the result of a cherry picked.
    /// </summary>
    public class CherryPickResult
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected CherryPickResult()
        { }

        internal CherryPickResult(CherryPickStatus status, Commit commit = null)
        {
            Commit = commit;
            Status = status;
        }

        /// <summary>
        /// The resulting commit of the cherry pick.
        /// <para>
        ///   This will return <code>null</code> if the cherry pick was not committed.
        ///     This can happen if:
        ///       1) The cherry pick resulted in conflicts.
        ///       2) The option to not commit on success is set.
        ///   </para>
        /// </summary>
        public virtual Commit Commit { get; private set; }

        /// <summary>
        /// The status of the cherry pick.
        /// </summary>
        public virtual CherryPickStatus Status { get; private set; }
    }

    /// <summary>
    /// The status of what happened as a result of a cherry-pick.
    /// </summary>
    public enum CherryPickStatus
    {
        /// <summary>
        /// The commit was successfully cherry picked.
        /// </summary>
        CherryPicked,

        /// <summary>
        /// The cherry pick resulted in conflicts.
        /// </summary>
        Conflicts
    }
}
