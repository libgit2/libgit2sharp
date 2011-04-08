using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class RepositorySafeHandle : SafeHandle
    {
        public RepositorySafeHandle() : base(IntPtr.Zero, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_repository_free(handle);
            return true;
        }

        public override bool IsInvalid
        {
            get { return (handle == IntPtr.Zero); }
        }
    }
}