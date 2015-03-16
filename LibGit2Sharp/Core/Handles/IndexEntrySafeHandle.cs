namespace LibGit2Sharp.Core.Handles
{
    internal class IndexEntrySafeHandle : NotOwnedSafeHandleBase
    {
        public GitIndexEntry MarshalAsGitIndexEntry()
        {
            return handle.MarshalAs<GitIndexEntry>();
        }
    }
}
