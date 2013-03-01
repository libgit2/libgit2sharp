namespace LibGit2Sharp.Core.Handles
{
    internal class TreeBuilderSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_treebuilder_free(handle);
            return true;
        }
    }
}
