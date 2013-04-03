using System;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitReferenceType
    {
        Invalid = 0,
        Oid = 1,
        Symbolic = 2,
        ListAll = Oid | Symbolic
    }
}
