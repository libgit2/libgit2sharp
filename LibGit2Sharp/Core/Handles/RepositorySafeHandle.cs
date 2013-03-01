namespace LibGit2Sharp.Core.Handles
{
    internal class RepositorySafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_repository_free(handle);
            return true;
        }
    }
}
