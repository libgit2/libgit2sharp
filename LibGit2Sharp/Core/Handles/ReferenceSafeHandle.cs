namespace LibGit2Sharp.Core.Handles
{
    internal class ReferenceSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_reference_free(handle);
            return true;
        }
    }
}
