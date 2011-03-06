using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_commit
    {
        public git_object commit;
        public git_vector parents;

        public IntPtr tree;
        public IntPtr author;
        public IntPtr committer;

        [MarshalAs(UnmanagedType.LPStr)]
        public string message;

        [MarshalAs(UnmanagedType.LPStr)]
        public string message_short;

        [MarshalAs(UnmanagedType.U1)]
        public bool full_parse;
    }
}
