namespace LibGit2Sharp.Core.Handles
{
    internal class BlameSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_blame_free(handle);
            return true;
        }
    }
}
