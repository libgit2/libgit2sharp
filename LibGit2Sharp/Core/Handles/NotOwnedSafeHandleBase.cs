using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal abstract class NotOwnedSafeHandleBase : SafeHandle
    {
        protected NotOwnedSafeHandleBase()
            : base(IntPtr.Zero, false)
        {
        }

        public override bool IsInvalid
        {
            get { return IsZero; }
        }

        public bool IsZero
        {
            get { return (handle == IntPtr.Zero); }
        }

        protected override bool ReleaseHandle()
        {
            // Does nothing as the pointer is owned by libgit2
            return true;
        }
    }
}
