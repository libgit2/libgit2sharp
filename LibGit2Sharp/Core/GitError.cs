using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitError
    {
        public IntPtr Message;
        public GitErrorCategory Category;
    }
}
