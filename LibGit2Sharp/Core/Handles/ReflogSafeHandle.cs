namespace LibGit2Sharp.Core.Handles
{
    internal class ReflogSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_reflog_free(handle);
            return true;
        }
    }
}
