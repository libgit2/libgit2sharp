namespace LibGit2Sharp.Core.Handles
{
    internal class TreeEntrySafeHandle_Owned : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            Proxy.git_tree_entry_free(handle);
            return true;
        }
    }
}
