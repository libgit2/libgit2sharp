using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitIndexerStats
    {
        public uint total;
        public uint processed;
    }
}
