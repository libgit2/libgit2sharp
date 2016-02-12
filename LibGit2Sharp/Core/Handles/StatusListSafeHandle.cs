namespace LibGit2Sharp.Core.Handles
{
    internal class StatusListSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_status_list_free(handle);
            return true;
        }
    }
}
