namespace LibGit2Sharp.Core.Handles
{
    internal class IndexSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_index_free(handle);
            return true;
        }
    }
}
