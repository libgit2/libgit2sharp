using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
            return (GitStatusEntry)Marshal.PtrToStructure(handle, typeof(GitStatusEntry));
        }
    }
}
