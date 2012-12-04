using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitIndexEntry
    {
        public GitIndexTime CTime;
        public GitIndexTime MTime;
        public uint Dev;
        public uint Ino;
        public uint Mode;
        public uint Uid;
        public uint Gid;
        public Int64 file_size;
        public GitOid oid;
        public ushort Flags;
        public ushort ExtendedFlags;
        public IntPtr Path;
    }
}
