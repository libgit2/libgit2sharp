using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitConfigEntryHandle : NotOwnedSafeHandleBase
    {
        public GitConfigEntry MarshalAsGitConfigEntry()
        {
            return (GitConfigEntry)Marshal.PtrToStructure(handle, typeof(GitConfigEntry));
        }
    }
}
