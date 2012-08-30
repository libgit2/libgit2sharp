using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class GitIndexerStats
    {
        public int Total;
        public int Processed;
        public int Received;
    }
}