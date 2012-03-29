namespace LibGit2Sharp.Core.Handles
{
    internal class RevWalkerSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            NativeMethods.git_revwalk_free(handle);
            return true;
        }
    }
}
