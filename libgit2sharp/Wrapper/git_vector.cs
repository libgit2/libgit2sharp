using System;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
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