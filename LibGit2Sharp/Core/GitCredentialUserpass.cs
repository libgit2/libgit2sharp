using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GitCredentialUserpass
    {
        public GitCredential parent;
        public char* username;
        public char* password;
    }
}

