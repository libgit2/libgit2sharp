using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitIndexerStats
    {
        public int Total;
        public int Processed;
        public int Received;
    }
}
