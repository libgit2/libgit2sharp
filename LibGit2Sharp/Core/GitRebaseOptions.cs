using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    struct GitRebaseOptions
    {
        public uint version;

        public int quiet;

        public IntPtr rewrite_notes_ref;
    }
}
