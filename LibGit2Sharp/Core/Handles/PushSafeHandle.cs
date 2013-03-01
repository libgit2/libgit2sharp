namespace LibGit2Sharp.Core.Handles
{
    internal class PushSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_push_free(handle);
            return true;
        }
    }
}
