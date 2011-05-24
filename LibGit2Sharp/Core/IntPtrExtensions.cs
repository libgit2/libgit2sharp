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
    }
}