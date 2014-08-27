using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class NotOwnedReferenceSafeHandle : NotOwnedSafeHandleBase
    {
        public NotOwnedReferenceSafeHandle(IntPtr handle)
            : base(handle)
        {
        }
    }
}
