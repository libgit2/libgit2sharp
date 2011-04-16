using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Blob : GitObject
    {
        internal Blob(IntPtr obj, ObjectId id)
            : base(obj, id)
        {
        }

        public int Size { get; set; }

        public byte[] Content
        {
            get
            {
                var ptr = NativeMethods.git_blob_rawcontent(Obj);
                var arr = new byte[Size];
                Marshal.Copy(ptr, arr, 0, Size);
                return arr;
            }
        }

        public Stream ContentStream
        {
            get
            {
                var ptr = NativeMethods.git_blob_rawcontent(Obj);
                unsafe
                {
                    return new UnmanagedMemoryStream((byte*)ptr.ToPointer(), Size);
                }
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

        public static Blob BuildFromPtr(IntPtr obj, ObjectId id)
        {
            var blob = new Blob(obj, id)
                           {
                               Size = NativeMethods.git_blob_rawsize(obj)
                           };
            return blob;
        }
    }
}