using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A git filter
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitFilter
    {
        public uint version;

        public IntPtr attributes;

        public IntPtr init;

        public IntPtr shutdown;
        
        public IntPtr check;

        public IntPtr apply;

        public IntPtr cleanup;
    }
}