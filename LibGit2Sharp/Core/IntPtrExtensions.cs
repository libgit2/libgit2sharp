using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal static class IntPtrExtensions
    {
        public static GitOid MarshalAsOid(this IntPtr intPtr)
        {
            return (GitOid)Marshal.PtrToStructure(intPtr, typeof(GitOid));
        }
    }
}
