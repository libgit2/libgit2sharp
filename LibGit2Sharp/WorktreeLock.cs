using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LibGit2Sharp
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class WorktreeLock
    {
        public WorktreeLock() : this(false, null)
        {

        }

        public WorktreeLock(bool isLocked, string reason)
        {
            IsLocked = isLocked;
            Reason = reason;
        }
        public virtual bool IsLocked { get; }
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
