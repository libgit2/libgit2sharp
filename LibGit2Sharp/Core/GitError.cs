using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class GitError
    {
        internal string Message;
        internal GitErrorClass Class;
    }
}