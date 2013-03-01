using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class NullIndexSafeHandle : IndexSafeHandle
    {
        public NullIndexSafeHandle()
        {
            handle = IntPtr.Zero;
        }

        protected override bool InternalReleaseHandle()
        {
            // Nothing to release
            return true;
        }
    }
}
