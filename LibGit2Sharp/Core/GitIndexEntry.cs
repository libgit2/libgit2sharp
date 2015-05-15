using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitIndexEntry
    {
        internal const ushort GIT_IDXENTRY_VALID = 0x8000;

        public GitIndexTime CTime;
        public GitIndexTime MTime;
        public uint Dev;
        public uint Ino;
        public uint Mode;
        public uint Uid;
        public uint Gid;
        public uint file_size;
        public GitOid Id;
        public ushort Flags;
        public ushort ExtendedFlags;
        public IntPtr Path;
    }
}
