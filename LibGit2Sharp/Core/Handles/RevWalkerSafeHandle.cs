namespace LibGit2Sharp.Core.Handles
{
    internal class RevWalkerSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_revwalk_free(handle);
            return true;
        }
    }
}
