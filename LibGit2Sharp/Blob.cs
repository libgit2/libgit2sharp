using System;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Blob : GitObject
    {
        private readonly IntPtr _intPtrBlob;

        internal Blob(IntPtr intPtrBlob, ObjectId id)
            : base(id)
        {
            _intPtrBlob = intPtrBlob;
        }

        public int Size { get { return NativeMethods.git_blob_rawsize(_intPtrBlob); } }

        public byte[] Content
        {
            get
            {
                var size = Size;
                var ptr = NativeMethods.git_blob_rawcontent(_intPtrBlob);
                var arr = new byte[size];
                Marshal.Copy(ptr, arr, 0, size);
                return arr;
            }
        }

        public string ContentAsUtf8()
        {
            return Encoding.UTF8.GetString(Content);
        }

        public string ContentAsUnicode()
        {
            return Encoding.Unicode.GetString(Content);
        }
    }
}