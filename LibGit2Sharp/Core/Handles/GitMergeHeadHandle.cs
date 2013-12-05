using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitMergeHeadHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_merge_head_free(handle);
            return true;
        }
    }
}
