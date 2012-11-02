using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class IndexEntrySafeHandle : NotOwnedSafeHandleBase
    {
        public GitIndexEntry MarshalAsGitIndexEntry()
        {
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            return (GitIndexEntry)Marshal.PtrToStructure(handle, typeof(GitIndexEntry));
        }
    }
}
