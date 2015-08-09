using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitObjectSafeHandle : SafeHandleBase
    {
        public GitObjectSafeHandle()
        { }

        public GitObjectSafeHandle(IntPtr invalidHandleValue) : base(invalidHandleValue)
        { }

        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_object_free(handle);
            return true;
        }
    }
}
