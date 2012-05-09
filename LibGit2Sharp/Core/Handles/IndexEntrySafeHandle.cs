using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class IndexEntrySafeHandle : NotOwnedSafeHandleBase
    {
        public GitIndexEntry MarshalAsGitIndexEntry()
        {
            return (GitIndexEntry)Marshal.PtrToStructure(handle, typeof(GitIndexEntry));
        }
    }
}
