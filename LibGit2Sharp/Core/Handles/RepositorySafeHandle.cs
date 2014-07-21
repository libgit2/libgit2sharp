using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class RepositorySafeHandle : SafeHandleBase
    {
        public RepositorySafeHandle()
            : base()
        {
        }

        public RepositorySafeHandle(IntPtr value)
            : base(ownsHandle: false)
        {
            SetHandle(value);
        }

        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_repository_free(handle);
            return true;
        }
    }
}
