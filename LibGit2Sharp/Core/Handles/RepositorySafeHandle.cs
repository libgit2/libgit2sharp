namespace LibGit2Sharp.Core.Handles
{
    internal class RepositorySafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            Proxy.git_repository_free(handle);
            return true;
        }
    }
}
