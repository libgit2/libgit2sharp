namespace LibGit2Sharp.Core.Handles
{
    internal class GitAnnotatedCommitHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_annotated_commit_free(handle);
            return true;
        }
    }
}
