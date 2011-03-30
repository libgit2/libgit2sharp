using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class GitSignature
    {
        public string Name;
        public string Email;
        public GitTime When;
    }
}