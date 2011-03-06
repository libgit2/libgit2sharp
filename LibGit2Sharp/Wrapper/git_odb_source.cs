using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_odb_source
    {
        public git_rawobj raw;
        public IntPtr write_ptr;
        public UIntPtr written_bytes;

        [MarshalAs(UnmanagedType.U1)]
        public bool open;
    }
}