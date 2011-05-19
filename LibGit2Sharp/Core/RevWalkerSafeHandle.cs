namespace LibGit2Sharp.Core
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