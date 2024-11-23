using System;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitCertificateSshType
    {
        MD5 = (1 << 0),
        SHA1 = (1 << 1),
    }
}
