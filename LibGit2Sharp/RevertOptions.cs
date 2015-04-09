using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Revert behavior.
    /// </summary>
    public sealed class RevertOptions : MergeAndCheckoutOptionsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RevertOptions"/> class.
        /// By default the revert will be committed if there are no conflicts.
        /// </summary>
        public RevertOptions()
        {
        }

        /// <summary>
        /// When reverting a merge commit, the parent number to consider as
        /// mainline, starting from offset 1.
        /// <para>
        ///  As a merge commit has multiple parents, reverting a merge commit
        ///  will reverse all the changes brought in by the merge except for
        ///  one parent's line of commits. The parent to preserve is called the
        ///  mainline, and must be specified by its number (i.e. offset).
        /// </para>
        /// </summary>
        public int Mainline { get; set; }
    }
}
