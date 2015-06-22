using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class RebaseSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_rebase_free(handle);
            return true;
        }
    }
}
