using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class GitTime
    {
        public long Time;
        public int Offset;
    }
}