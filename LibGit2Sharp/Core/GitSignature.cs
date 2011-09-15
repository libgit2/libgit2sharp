using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSignature
    {
        public string Name;
        public string Email;
        public GitTime When;
    }
}
