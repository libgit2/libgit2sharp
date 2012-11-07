using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitConfigEntry
    {
        public IntPtr namePtr;
        public IntPtr valuePtr;
        public uint level;
    }
}
