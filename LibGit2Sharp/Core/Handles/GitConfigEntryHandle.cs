namespace LibGit2Sharp.Core.Handles
{
    internal class GitConfigEntryHandle : SafeHandleBase
    {
        public GitConfigEntry MarshalAsGitConfigEntry()
        {
            return handle.MarshalAs<GitConfigEntry>();
        }

        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_config_entry_free(handle);
            return true;
        }
    }
}
