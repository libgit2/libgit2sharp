using System;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitBranchType
    {
        GIT_BRANCH_LOCAL = 1,
        GIT_BRANCH_REMOTE = 2,
        GIT_BRANCH_ALL = GIT_BRANCH_LOCAL | GIT_BRANCH_REMOTE,
    }
}
