using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitCredentialUserpass
    {
        public GitCredential parent;
        public IntPtr username;
        public IntPtr password;
    }
}

