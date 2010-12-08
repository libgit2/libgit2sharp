using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_rawobj
    {
        public IntPtr data;          /**< Raw, decompressed object data. */
        public UIntPtr len;          /**< Total number of bytes in data. */
        public git_otype type;      /**< Type of this object. */

        internal RawObject Build(string objectId)
        {
            var header = BuildHeader(objectId);
            var rawData = new byte[header.Length];

            //TODO: Casting the length to an int may lead to not copy the whole data. This should be converted to a loop. 
            //see http://stackoverflow.com/questions/1087982/single-objects-still-limited-to-2-gb-in-size-in-clr-4-0 and http://blogs.msdn.com/b/joshwil/archive/2005/08/10/450202.aspx for further inspiration.
            Debug.Assert(header.Length < int.MaxValue);

            Marshal.Copy(this.data, rawData, 0, (int)header.Length);
            return new RawObject(header, rawData);
        }

        internal Header BuildHeader(string objectId)
        {
            var header = new Header(objectId, (ObjectType)type, len.ToUInt64());
            return header;
        }
    }
}