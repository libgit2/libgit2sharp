namespace LibGit2Sharp.Core.Handles
{
    internal class ConfigurationIteratorSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_config_iterator_free(handle);
            return true;
        }
    }
}
