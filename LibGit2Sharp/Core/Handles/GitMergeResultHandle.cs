using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitMergeResultHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_merge_result_free(handle);
            return true;
        }
    }
}
