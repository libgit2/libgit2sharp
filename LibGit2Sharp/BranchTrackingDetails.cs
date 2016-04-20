namespace LibGit2Sharp
{
    /// <summary>
    /// Tracking information for a <see cref="Branch"/>
    /// </summary>
    public class BranchTrackingDetails
    {
        private readonly HistoryDivergence historyDivergence;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected BranchTrackingDetails()
        { }

        internal BranchTrackingDetails(Repository repo, Branch branch)
        {
            if (!branch.IsTracking || branch.Tip == null || branch.TrackedBranch.Tip == null)
            {
                historyDivergence = new NullHistoryDivergence();
                return;
            }

            historyDivergence = repo.ObjectDatabase.CalculateHistoryDivergence(branch.Tip, branch.TrackedBranch.Tip);
        }

        /// <summary>
        /// Gets the number of commits that exist in this local branch but don't exist in the tracked one.
        /// <para>
        ///   This property will return <c>null</c> if this local branch has no upstream configuration
        ///   or if the upstream branch does not exist
        /// </para>
        /// </summary>
        public virtual int? AheadBy
        {
            get { return historyDivergence.AheadBy; }
        }

        /// <summary>
        /// Gets the number of commits that exist in the tracked branch but don't exist in this local one.
        /// <para>
        ///   This property will return <c>null</c> if this local branch has no upstream configuration
        ///   or if the upstream branch does not exist
        /// </para>
        /// </summary>
        public virtual int? BehindBy
        {
            get { return historyDivergence.BehindBy; }
        }

        /// <summary>
        /// Gets the common ancestor of the local branch and its tracked remote branch.
        /// <para>
        ///   This property will return <c>null</c> if this local branch has no upstream configuration,
        ///   the upstream branch does not exist, or either branch is an orphan.
        /// </para>
        /// </summary>
        public virtual Commit CommonAncestor
        {
            get { return historyDivergence.CommonAncestor; }
        }
    }
}
