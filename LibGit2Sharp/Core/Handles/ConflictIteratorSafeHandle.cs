namespace LibGit2Sharp.Core.Handles
{
    internal class ConflictIteratorSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_index_conflict_iterator_free(handle);
            return true;
        }
    }
}
