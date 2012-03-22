namespace LibGit2Sharp.Core.Handles
{
    internal class DiffListSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            NativeMethods.git_diff_list_free(handle);
            return true;
        }
    }
}
