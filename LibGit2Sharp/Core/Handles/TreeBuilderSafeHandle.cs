namespace LibGit2Sharp.Core.Handles
{
    internal class TreeBuilderSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            NativeMethods.git_treebuilder_free(handle);
            return true;
        }
    }
}
