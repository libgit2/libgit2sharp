namespace LibGit2Sharp.Core.Handles
{
    internal class GitConfigEntryHandle : NotOwnedSafeHandleBase
    {
        public GitConfigEntry MarshalAsGitConfigEntry()
        {
            return handle.MarshalAs<GitConfigEntry>();
        }
    }
}
