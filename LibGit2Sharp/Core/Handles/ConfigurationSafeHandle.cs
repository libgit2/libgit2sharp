namespace LibGit2Sharp.Core.Handles
{
    internal class ConfigurationSafeHandle : SafeHandleBase
    {
        protected override bool InternalReleaseHandle()
        {
            Proxy.git_config_free(handle);
            return true;
        }
    }
}
