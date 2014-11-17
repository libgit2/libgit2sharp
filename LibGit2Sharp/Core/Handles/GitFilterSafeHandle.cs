using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitFilterSafeHandle : NotOwnedSafeHandleBase
    {
        internal GitFilterSafeHandle(GitFilter gitFilter)
        {
            handle = Marshal.AllocHGlobal(Marshal.SizeOf(gitFilter));
            Marshal.StructureToPtr(gitFilter, handle, false);
        }

        internal GitFilterSafeHandle(IntPtr intPtr)
        {
            handle = intPtr;
        }

        public GitFilter MarshalFromNative()
        {
            return handle.MarshalAs<GitFilter>();
        }
    }
}