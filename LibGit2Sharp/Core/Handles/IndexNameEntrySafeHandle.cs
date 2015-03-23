namespace LibGit2Sharp.Core.Handles
{
    internal class IndexNameEntrySafeHandle : NotOwnedSafeHandleBase
    {
        public GitIndexNameEntry MarshalAsGitIndexNameEntry()
        {
            return handle.MarshalAs<GitIndexNameEntry>();
        }
    }
}
