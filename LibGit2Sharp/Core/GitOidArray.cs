using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitOidArray
    {
        /// <summary>
        /// A pointer to an array of ids.
        /// </summary>
        public IntPtr Ids;

        /// <summary>
        /// The number of ids in the array.
        /// </summary>
        public UIntPtr Length;

        /// <summary>
        /// Resets the GitOidArray to default values.
        /// </summary>
        public void Reset()
        {
            Ids = IntPtr.Zero;
            Length = UIntPtr.Zero;
        }
    }
}
