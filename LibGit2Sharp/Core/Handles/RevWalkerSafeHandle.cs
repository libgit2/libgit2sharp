namespace LibGit2Sharp.Core.Handles
{
    internal class RevWalkerSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            Proxy.git_revwalk_free(handle);
            return true;
        }
    }
}
