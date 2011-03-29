using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    [StructLayout(LayoutKind.Sequential)]
    public class GitTime
    {
        public long Time;
        public int Offset;
    }
}