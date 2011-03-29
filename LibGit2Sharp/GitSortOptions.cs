using System;

namespace LibGit2Sharp
{
    [Flags]
    public enum GitSortOptions
    {
        None = 0,
        Topo = (1 << 0),
        Time = (1 << 1),
        Reverse = (1 << 2)
    }
}