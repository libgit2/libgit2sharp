using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitWriteStream
    {
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public write_fn write;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public close_fn close;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public free_fn free;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int write_fn(IntPtr stream, IntPtr buffer, UIntPtr len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int close_fn(IntPtr stream);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_fn(IntPtr stream);
    }
}
