using System;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_repository
    {
        public IntPtr database;
        public IntPtr index;
        public IntPtr objects;

        [MarshalAs(UnmanagedType.LPStr)]
        public string path_repository;
     
        [MarshalAs(UnmanagedType.LPStr)]
        public string path_index;
        
        [MarshalAs(UnmanagedType.LPStr)]
        public string path_odb;
        
        [MarshalAs(UnmanagedType.LPStr)]
        public string path_workdir;

        [MarshalAs(UnmanagedType.U1)]
        public bool is_bare;
    }
}