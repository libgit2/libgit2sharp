using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{

    [StructLayout(LayoutKind.Sequential)]
    internal class GitRefdbIterator
    {
        static GitRefdbIterator()
        {
            GCHandleOffset = Marshal.OffsetOf(typeof(GitRefdbIterator), "GCHandle").ToInt32();
        }

        IntPtr refDb;

        public ref_db_next next;
        public ref_db_next_name next_name;
        public ref_db_free free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        public static int GCHandleOffset;

        public IntPtr RefNamePtr;

        internal delegate int ref_db_next(
            out IntPtr reference,
            IntPtr iter);

        internal delegate int ref_db_next_name(
            out IntPtr refNamePtr,
            IntPtr iter);

        internal delegate void ref_db_free(IntPtr iter);
    }
}
