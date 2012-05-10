using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class GitError
    {
        public string Message;
        public GitErrorClass Class;
    }
}
