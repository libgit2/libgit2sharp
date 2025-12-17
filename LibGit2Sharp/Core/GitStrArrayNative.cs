using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A git_strarray where the string array and strings themselves were allocated
    /// with libgit2's allocator. Only libgit2 can free this git_strarray.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitStrArrayNative : IDisposable
    {
        public GitStrArray Array;

        /// <summary>
        /// Enumerates each string from the array using the UTF-8 marshaler.
        /// </summary>
        public string[] ReadStrings()
        {
            var count = checked((int)Array.Count.ToUInt32());

            string[] toReturn = new string[count];

            for (int i = 0; i < count; i++)
            {
                toReturn[i] = LaxUtf8Marshaler.FromNative(Marshal.ReadIntPtr(Array.Strings, i * IntPtr.Size));
            }

            return toReturn;
        }

        public void Dispose()
        {
            if (Array.Strings != IntPtr.Zero)
            {
                NativeMethods.git_strarray_free(ref Array);
            }

            // Now that we've freed the memory, zero out the structure.
            Array.Reset();
        }
    }
}
