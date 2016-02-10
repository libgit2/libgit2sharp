using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSmartSubtransportStream
    {
        static GitSmartSubtransportStream()
        {
            GCHandleOffset = Marshal.OffsetOf(typeof(GitSmartSubtransportStream), "GCHandle").ToInt32();
        }

        public IntPtr SmartTransport;

        public read_callback Read;
        public write_callback Write;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        public delegate int read_callback(
            IntPtr stream,
            IntPtr buffer,
            UIntPtr buf_size,
            out UIntPtr bytes_read);

        public delegate int write_callback(
            IntPtr stream,
            IntPtr buffer,
            UIntPtr len);

        public delegate void free_callback(IntPtr stream);
    }
}
