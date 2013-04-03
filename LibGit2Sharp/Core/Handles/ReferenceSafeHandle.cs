using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class ReferenceSafeHandle : SafeHandleBase
    {
        public ReferenceSafeHandle() : base()
        {
        }

        public ReferenceSafeHandle(IntPtr ptr, bool free) : base(ptr, free)
        {
        }

        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_reference_free(handle);
            return true;
        }
    }
}
