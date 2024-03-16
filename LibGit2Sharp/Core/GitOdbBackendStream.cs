using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitOdbBackendStreamMode
    {
        Read = 2,
        Write = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitOdbBackendStream
    {
        static GitOdbBackendStream()
        {
            GCHandleOffset = Marshal.OffsetOf<GitOdbBackendStream>(nameof(GCHandle)).ToInt32();
        }

        public IntPtr Backend;
        public GitOdbBackendStreamMode Mode;
        public IntPtr HashCtx;

        public long DeclaredSize;
        public long ReceivedBytes;

        public read_callback Read;
        public write_callback Write;
        public finalize_write_callback FinalizeWrite;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int read_callback(
            IntPtr stream,
            IntPtr buffer,
            UIntPtr len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int write_callback(
            IntPtr stream,
            IntPtr buffer,
            UIntPtr len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int finalize_write_callback(IntPtr stream, ref GitOid oid);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(IntPtr stream);
    }
}
