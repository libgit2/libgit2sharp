namespace LibGit2Sharp.Core.Handles
{
    internal class PackBuilderSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_packbuilder_free(handle);
            return true;
        }
    }
}
