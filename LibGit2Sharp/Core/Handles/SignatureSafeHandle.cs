namespace LibGit2Sharp.Core.Handles
{
    internal class SignatureSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_signature_free(handle);
            return true;
        }
    }
}
