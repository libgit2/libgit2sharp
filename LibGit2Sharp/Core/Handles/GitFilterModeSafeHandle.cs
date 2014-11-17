using System;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitFilterModeSafeHandle : NotOwnedSafeHandleBase
    {
        internal GitFilterModeSafeHandle(IntPtr intPtr)
        {
            handle = intPtr;
        }

        public GitFilterSource MarshalFromNative()
        {
            return handle.MarshalAs<GitFilterSource>();
        }
    }
}