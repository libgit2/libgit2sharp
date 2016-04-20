using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitIndexTime
    {
        public int seconds;
        public uint nanoseconds;
    }
}
