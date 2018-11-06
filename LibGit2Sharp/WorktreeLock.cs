using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///     Represents the lock state of a Worktree
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class WorktreeLock
    {
        /// <summary>
        ///     Creates a new instance of <see cref="WorktreeLock"/> with default, unlocked, state
        /// </summary>
        public WorktreeLock() : this(false, null)
        {

        }

        /// <summary>
        ///     Creates a new instance of <see cref="WorktreeLock"/>
        /// </summary>
        /// <param name="isLocked">the locked state</param>
        /// <param name="reason">the reason given for the lock</param>
        public WorktreeLock(bool isLocked, string reason)
        {
            IsLocked = isLocked;
            Reason = reason;
        }
        /// <summary>
        ///     Gets a flag indicating if the worktree is locked
        /// </summary>
        public virtual bool IsLocked { get; }

        /// <summary>
        ///     Gets the reason, if set, for the lock
        /// </summary>
        public virtual string Reason { get; }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} => {1}", IsLocked, Reason);
            }
        }
    }
}
