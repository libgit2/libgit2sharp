using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitStrArray
    {
        /// <summary>
        /// A pointer to an array of null-terminated strings.
        /// </summary>
        public IntPtr Strings;

        /// <summary>
        /// The number of strings in the array.
        /// </summary>
        public UIntPtr Count;

        /// <summary>
        /// Resets the GitStrArray to default values.
        /// </summary>
        public void Reset()
        {
            Strings = IntPtr.Zero;
            Count = UIntPtr.Zero;
        }
    }
}
