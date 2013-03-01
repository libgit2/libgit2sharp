namespace LibGit2Sharp.Core.Handles
{
    internal class GitObjectSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_object_free(handle);
            return true;
        }
    }
}
