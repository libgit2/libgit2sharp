namespace LibGit2Sharp.Core
{
    internal unsafe struct git_rebase_operation
    {
        internal RebaseStepOperation type;
        internal GitOid id;
        internal char* exec;
    }
}
