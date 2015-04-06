using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// The status of what happened as a result of a stash application.
    /// </summary>
    public enum StashApplyStatus
    {
        /// <summary>
        /// The changes were successfully stashed.
        /// </summary>
        Applied,

        /// <summary>
        /// The stash application resulted in conflicts.
        /// </summary>
        Conflicts,

        /// <summary>
        /// The stash application was not applied due to existing
        /// untracked files that would be overwritten by the stash
        /// contents.
        /// </summary>
        UntrackedExist,
    }
}
