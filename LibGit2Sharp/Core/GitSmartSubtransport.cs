using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSmartSubtransport
    {
        static GitSmartSubtransport()
        {
            GCHandleOffset = Marshal.OffsetOf<GitSmartSubtransport>(nameof(GCHandle)).ToInt32();
        }

        public action_callback Action;
        public close_callback Close;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int action_callback(
            out IntPtr stream,
            IntPtr subtransport,
            IntPtr url,
            GitSmartSubtransportAction action);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int close_callback(IntPtr subtransport);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(IntPtr subtransport);
    }
}
