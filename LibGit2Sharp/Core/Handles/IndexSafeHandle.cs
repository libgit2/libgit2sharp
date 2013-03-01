namespace LibGit2Sharp.Core.Handles
{
    internal class IndexSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_index_free(handle);
            return true;
        }
    }
}
