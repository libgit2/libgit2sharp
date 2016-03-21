using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_index_mtime
    {
        public int seconds;
        public uint nanoseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_index_entry
    {
        internal const ushort GIT_IDXENTRY_VALID = 0x8000;

        public git_index_mtime ctime;
        public git_index_mtime mtime;
        public uint dev;
        public uint ino;
        public uint mode;
        public uint uid;
        public uint gid;
        public uint file_size;
        public git_oid id;
        public ushort flags;
        public ushort extended_flags;
        public char* path;
    }
}
