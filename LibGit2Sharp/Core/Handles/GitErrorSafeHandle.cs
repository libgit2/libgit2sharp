using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitErrorSafeHandle : NotOwnedSafeHandleBase
    {
        public GitError MarshalAsGitError()
        {
            return (GitError)Marshal.PtrToStructure(handle, typeof(GitError));
        }
    }
}
