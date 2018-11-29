using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GitConfigEntry
    {
        public char* namePtr;
        public char* valuePtr;
        public uint include_depth;
        public uint level;
        public void* freePtr;
        public void* payloadPtr;
    }
}
