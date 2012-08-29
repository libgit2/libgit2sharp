namespace LibGit2Sharp.Core.Handles
{
    internal class RemoteSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            Proxy.git_remote_free(handle);
            return true;
        }
    }
}
