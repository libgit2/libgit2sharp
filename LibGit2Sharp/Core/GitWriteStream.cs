using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitWriteStream
    {
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public write_fn write;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public close_fn close;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public free_fn free;

        public delegate int write_fn(IntPtr stream, IntPtr buffer, UIntPtr len);
        public delegate int close_fn(IntPtr stream);
        public delegate void free_fn(IntPtr stream);
    }
}
