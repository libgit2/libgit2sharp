using System;
using System.Runtime.InteropServices;

using LibGit2Sharp;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSmartSubtransport
    {
        public delegate int action_callback(
            out IntPtr stream,
            IntPtr subtransport,
            IntPtr url,
            SmartSubtransportAction action);

        public delegate int close_callback(IntPtr subtransportPtr);
        public delegate int free_callback(IntPtr subtransportPtr);

        // Because our GitSmartSubtransport structure exists on the managed heap only for a short time (to be marshaled
        // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
        // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
        // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.
        public action_callback Action;
        public close_callback Close;
        public free_callback Free;

        // The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping.

        public IntPtr GCHandle;

        public static int GCHandleOffset = Marshal.OffsetOf(typeof(GitSmartSubtransport), "GCHandle").ToInt32();
    }
}
