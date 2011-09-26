using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitIndexTime
    {
        public long seconds;
        public uint nanoseconds;
    }
}
