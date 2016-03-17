using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// The results of a merge of two trees.
    /// </summary>
    public class MergeTreeResult
    {
        /// <summary>
        /// Create a <see cref="LibGit2Sharp.MergeTreeResult"/> which resulted in conflicts
        /// </summary>
        /// <param name="conflicts">The list of conflicts from the merge</param>
        public MergeTreeResult(IEnumerable<Conflict> conflicts)
        {
            this.Status = MergeTreeStatus.Conflicts;
            this.Conflicts = conflicts;
        }

        /// <summary>
        /// Create a <see cref="LibGit2Sharp.MergeTreeResult"/> from a successful merge
        /// </summary>
        /// <param name="tree">The resulting tree</param>
        public MergeTreeResult(Tree tree)
        {
            this.Status = MergeTreeStatus.Succeeded;
            this.Tree = tree;
            this.Conflicts = new List<Conflict>();
        }

        /// <summary>
        /// The status of the merge.
        /// </summary>
        public virtual MergeTreeStatus Status { get; private set; }

        /// <summary>
        /// The resulting tree of the merge.
        /// <para>This will return <code>null</code> if the merge has been unsuccessful due to conflicts.</para>
        /// </summary>
        public virtual Tree Tree { get; private set; }

        /// <summary>
        /// The resulting conflicts from the merge.
        /// <para>This will return <code>null</code> if the merge was successful and there were no conflicts.</para>
        /// </summary>
        public virtual IEnumerable<Conflict> Conflicts { get; private set; }
    }

    /// <summary>
    /// The status of what happened as a result of a merge.
    /// </summary>
    public enum MergeTreeStatus
    {
        /// <summary>
        /// Merge succeeded.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Merge resulted in conflicts.
        /// </summary>
        Conflicts,
    }
}
