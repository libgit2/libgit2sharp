using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal static class MarshalPortable
    {
        internal static int SizeOf<T>()
        {
#if NET40
            return Marshal.SizeOf(typeof(T));
#else
            return Marshal.SizeOf<T>();
#endif
        }

        internal static IntPtr OffsetOf<T>(string fieldName)
        {
#if NET40
            return Marshal.OffsetOf(typeof(T), fieldName);
#else
            return Marshal.OffsetOf<T>(fieldName);
#endif
        }

        internal static T PtrToStructure<T>(IntPtr ptr)
        {
#if NET40
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
#else
            return Marshal.PtrToStructure<T>(ptr);
#endif
        }
    }
}
