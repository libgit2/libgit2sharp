namespace LibGit2Sharp.Core.Handles
{
    internal class DiffSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_diff_free(handle);
            return true;
        }
    }
}
