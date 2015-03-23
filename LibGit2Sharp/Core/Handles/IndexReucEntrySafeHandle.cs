namespace LibGit2Sharp.Core.Handles
{
    internal class IndexReucEntrySafeHandle : NotOwnedSafeHandleBase
    {
        public GitIndexReucEntry MarshalAsGitIndexReucEntry()
        {
            return handle.MarshalAs<GitIndexReucEntry>();
        }
    }
}
