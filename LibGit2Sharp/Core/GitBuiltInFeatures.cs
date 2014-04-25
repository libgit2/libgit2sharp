using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Flags to indentify libgit compiled features.
    /// </summary>
    [Flags]
    internal enum GitBuiltInFeatures
    {
        Threads = (1 << 0),
        Https = (1 << 1),
        Ssh = (1 << 2),
    }
}
