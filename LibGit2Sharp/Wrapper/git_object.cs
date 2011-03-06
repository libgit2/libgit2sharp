using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_object
    {
        public git_oid id;
        public IntPtr repo;
        public git_odb_source source;

        [MarshalAs(UnmanagedType.U1)]
        public bool in_memory;

        [MarshalAs(UnmanagedType.U1)]
        public bool modified;
    }
}