using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class NullGitObjectSafeHandle : GitObjectSafeHandle
    {
        public NullGitObjectSafeHandle()
        {
            handle = IntPtr.Zero;
        }

        protected override bool ReleaseHandle()
        {
            // Nothing to release
            return true;
        }
    }
}
