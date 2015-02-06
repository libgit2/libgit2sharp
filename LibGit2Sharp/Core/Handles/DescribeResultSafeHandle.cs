namespace LibGit2Sharp.Core.Handles
{
    internal class DescribeResultSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_describe_free(handle);
            return true;
        }
    }
}
