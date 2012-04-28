namespace LibGit2Sharp.Core.Handles
{
    internal class GitObjectSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            NativeMethods.git_object_free(handle);
            return true;
        }
    }
}
