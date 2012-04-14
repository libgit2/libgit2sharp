using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal abstract class SafeHandleBase : SafeHandle
    {
        protected SafeHandleBase()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return (handle == IntPtr.Zero); }
        }

        protected abstract override bool ReleaseHandle();
    }
}
