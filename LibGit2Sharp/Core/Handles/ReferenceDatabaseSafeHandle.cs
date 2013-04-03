namespace LibGit2Sharp.Core.Handles
{
    internal class ReferenceDatabaseSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_refdb_free(handle);
            return true;
        }
    }
}

