using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int git_apply_delta_cb(IntPtr delta, IntPtr payload);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int git_apply_hunk_cb(IntPtr hunk, IntPtr payload);

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffApplyOpts
    {
        public uint Version = 1;
        public git_apply_delta_cb DeltaCallback;
        public git_apply_hunk_cb HunkCallback;
        public IntPtr Payload;
        public uint Flags = 0;
    }
}
