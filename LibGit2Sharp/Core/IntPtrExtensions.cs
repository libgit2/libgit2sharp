using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal static class IntPtrExtensions
    {
        public static string MarshallAsString(this IntPtr intPtr)
        {
            return Marshal.PtrToStringAnsi(intPtr);
        }

        public static FilePath MarshallAsFilePath(this IntPtr intPtr)
        {
            return intPtr.MarshallAsString();
        }

        public static GitOid MarshalAsOid(this IntPtr intPtr)
        {
            return (GitOid)Marshal.PtrToStructure(intPtr, typeof(GitOid));
        }
    }
}
