namespace LibGit2Sharp.Core.Handles
{
    internal class GitObjectSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_object_free(handle);
            return true;
        }
    }
}
