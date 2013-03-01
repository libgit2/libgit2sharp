namespace LibGit2Sharp.Core.Handles
{
    internal class RevWalkerSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_revwalk_free(handle);
            return true;
        }
    }
}
