using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class StatusEntrySafeHandle : NotOwnedSafeHandleBase
    {
        public StatusEntrySafeHandle()
            : base()
        {
        }

        public StatusEntrySafeHandle(IntPtr handle)
            : base()
        {
            this.SetHandle(handle);
        }

        public GitStatusEntry MarshalAsGitStatusEntry()
        {
            return handle.MarshalAs<GitStatusEntry>();
        }
    }
}
