namespace LibGit2Sharp.Core
{
    internal enum GitErrorCode
    {
        GIT_OK = 0,
        GIT_ERROR = -1,
        GIT_ENOTFOUND = -3,
        GIT_EEXISTS = -4,
        GIT_EAMBIGUOUS = -5,
        GIT_EBUFS = -6,
        GIT_EPASSTHROUGH = -30,
        GIT_EREVWALKOVER = -31,
    }
}
