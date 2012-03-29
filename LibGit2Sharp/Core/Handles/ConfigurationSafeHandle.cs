namespace LibGit2Sharp.Core.Handles
{
    internal class ConfigurationSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            NativeMethods.git_config_free(handle);
            return true;
        }
    }
}
