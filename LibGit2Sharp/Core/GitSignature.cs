using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSignature
    {
        public IntPtr Name;
        public IntPtr Email;
        public GitTime When;
    }
}
