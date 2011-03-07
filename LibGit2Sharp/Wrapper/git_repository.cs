using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_repository
    {
        public IntPtr database;
        public IntPtr index;

        public IntPtr objects;
        public git_vector memory_objects;

        public git_refcache references;

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

        [MarshalAs(UnmanagedType.U1)]
        public bool gc_enabled;

        internal RepositoryDetails Build()
        {
            return new RepositoryDetails(path_repository, path_index, path_odb, path_workdir, is_bare);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct git_refcache
    {
        public IntPtr packfile;
        public IntPtr loose_cache;
    }
}