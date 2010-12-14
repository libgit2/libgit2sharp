using System;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_tag
    {
        public git_object tag;
        public IntPtr target;
        public git_otype type;

        [MarshalAs(UnmanagedType.LPStr)]
        public string tag_name;

        public IntPtr tagger;

        [MarshalAs(UnmanagedType.LPStr)]
        public string message;
    }
}