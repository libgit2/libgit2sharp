using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling CherryPick behavior.
    /// </summary>
    public sealed class CherryPickOptions : MergeAndCheckoutOptionsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CherryPickOptions"/> class.
        /// By default the cherry pick will be committed if there are no conflicts.
        /// </summary>
        public CherryPickOptions()
        {
        }

        /// <summary>
        /// When cherry picking a merge commit, the parent number to consider as
        /// mainline, starting from offset 1.
        /// <para>
        ///  As a merge commit has multiple parents, cherry picking a merge commit
        ///  will take only the changes relative to the given parent.  The parent
        ///  to consider changes based on is called the mainline, and must be
        ///  specified by its number (i.e. offset).
        /// </para>
        /// </summary>
        public int Mainline { get; set; }
    }
}
