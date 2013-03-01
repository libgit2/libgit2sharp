namespace LibGit2Sharp.Core.Handles
{
    internal class RemoteSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_remote_free(handle);
            return true;
        }
    }
}
