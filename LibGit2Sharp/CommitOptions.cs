using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides optional additional information to commit creation.
    /// By default, a new commit will be created (instead of amending the
    /// HEAD commit) and an empty commit which is unchanged from the current
    /// HEAD is disallowed.
    /// </summary>
    public sealed class CommitOptions
    {
        /// <summary>
        /// True to amend the current <see cref="Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.
        /// </summary>
        public bool AmendPreviousCommit { get; set; }

        /// <summary>
        /// True to allow creation of an empty <see cref="Commit"/>, false otherwise.
        /// </summary>
        public bool AllowEmptyCommit { get; set; }
    }
}
