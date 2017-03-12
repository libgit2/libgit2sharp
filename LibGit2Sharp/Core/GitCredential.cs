using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitCredential
    {
        public GitCredentialType credtype;
        public IntPtr free;
    }
}

