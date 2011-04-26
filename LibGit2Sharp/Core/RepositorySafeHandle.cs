using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal class RepositorySafeHandle : SafeHandle
    {
        public RepositorySafeHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return (handle == IntPtr.Zero); }
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_repository_free(handle);
            return true;
        }
    }
}