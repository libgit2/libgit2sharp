using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_signature
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;

        [MarshalAs(UnmanagedType.LPStr)]
        public string email;

        public ulong time;
        public int offset;
    }
}