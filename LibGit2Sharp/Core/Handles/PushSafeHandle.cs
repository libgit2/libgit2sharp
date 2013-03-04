namespace LibGit2Sharp.Core.Handles
{
    internal class PushSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_push_free(handle);
            return true;
        }
    }
}
