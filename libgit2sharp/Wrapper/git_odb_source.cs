using System;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
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