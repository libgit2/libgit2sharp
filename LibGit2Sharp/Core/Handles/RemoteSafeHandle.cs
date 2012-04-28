﻿namespace LibGit2Sharp.Core.Handles
{
    internal class RemoteSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            NativeMethods.git_remote_free(handle);
            return true;
        }
    }
}
