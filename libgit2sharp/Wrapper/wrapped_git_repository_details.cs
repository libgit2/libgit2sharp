using System;
using System.Runtime.InteropServices;

namespace libgit2net.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct wrapped_git_repository_details
    {
        public IntPtr path_repository;
        public IntPtr path_index;
        public IntPtr path_odb;
        public IntPtr path_workdir;
        public IntPtr repository;

        [MarshalAs(UnmanagedType.U1)]
        public bool is_bare;
    }
}