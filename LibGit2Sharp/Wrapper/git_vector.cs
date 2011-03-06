using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_vector
    {
        public uint _alloc_size;

        public IntPtr _cmp;
        public IntPtr _srch;
        public IntPtr contents;

        public uint length;
    }
}