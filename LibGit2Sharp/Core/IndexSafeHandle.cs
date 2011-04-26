using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal class IndexSafeHandle : SafeHandle
    {
        public IndexSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return (handle == IntPtr.Zero); }
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_index_free(handle);
            return true;
        }
    }
}