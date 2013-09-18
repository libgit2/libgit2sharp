using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    /// Tracking information for a <see cref="Branch"/>
    /// </summary>
    public class BranchTrackingDetails
    {
        private readonly Repository repo;
        private readonly Branch branch;
        private readonly Lazy<Tuple<int?, int?>> aheadBehind;
        private readonly Lazy<Commit> commonAncestor;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected BranchTrackingDetails()
        { }

        internal BranchTrackingDetails(Repository repo, Branch branch)
        {
            this.repo = repo;
            this.branch = branch;

            aheadBehind = new Lazy<Tuple<int?, int?>>(ResolveAheadBehind);
            commonAncestor = new Lazy<Commit>(ResolveCommonAncestor);
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
            get { return aheadBehind.Value.Item1; }
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
            get { return aheadBehind.Value.Item2; }
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
            get { return commonAncestor.Value; }
        }

        private Tuple<int?, int?> ResolveAheadBehind()
        {
            return branch.IsTracking
                       ? Proxy.git_graph_ahead_behind(repo.Handle, branch.TrackedBranch.Tip, branch.Tip)
                       : new Tuple<int?, int?>(null, null);
        }

        private Commit ResolveCommonAncestor()
        {
            if (!branch.IsTracking)
            {
                return null;
            }

            if (branch.Tip == null || branch.TrackedBranch.Tip == null)
            {
                return null;
            }

            return repo.Commits.FindCommonAncestor(branch.Tip, branch.TrackedBranch.Tip);
        }
    }
}
