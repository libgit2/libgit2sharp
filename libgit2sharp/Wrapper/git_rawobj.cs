using System;
using System.Runtime.InteropServices;
using System.Text;

namespace libgit2net.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_rawobj
    {
        public IntPtr data;          /**< Raw, decompressed object data. */
        public UIntPtr len;          /**< Total number of bytes in data. */
        public git_otype type;      /**< Type of this object. */
    }
}