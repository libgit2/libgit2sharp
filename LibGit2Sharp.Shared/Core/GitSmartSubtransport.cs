using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSmartSubtransport
    {
        static GitSmartSubtransport()
        {
            GCHandleOffset = Marshal.OffsetOf(typeof(GitSmartSubtransport), "GCHandle").ToInt32();
        }

        public action_callback Action;
        public close_callback Close;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        public delegate int action_callback(
            out IntPtr stream,
            IntPtr subtransport,
            IntPtr url,
            GitSmartSubtransportAction action);

        public delegate int close_callback(IntPtr subtransport);

        public delegate void free_callback(IntPtr subtransport);
    }
}
