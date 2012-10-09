using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitIndexerStats
    {
        public uint Total;
        public uint Processed;
        public uint Received;
    }
}
