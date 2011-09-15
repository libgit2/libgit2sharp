using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Blob : GitObject
    {
        private readonly Repository repo;

        internal Blob(Repository repo, ObjectId id)
            : base(id)
        {
            this.repo = repo;
        }

        public int Size { get; set; }

        public byte[] Content
        {
            get
            {
                using (var obj = new ObjectSafeWrapper(Id, repo))
                {
                    var arr = new byte[Size];
                    Marshal.Copy(NativeMethods.git_blob_rawcontent(obj.ObjectPtr), arr, 0, Size);
                    return arr;
                }
            }
        }

        public Stream ContentStream
        {
            get
            {
                using (var obj = new ObjectSafeWrapper(Id, repo))
                {
                    IntPtr ptr = NativeMethods.git_blob_rawcontent(obj.ObjectPtr);
                    unsafe
                    {
                        return new UnmanagedMemoryStream((byte*)ptr.ToPointer(), Size);
                    }
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

        public static Blob BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var blob = new Blob(repo, id)
                           {
                               Size = NativeMethods.git_blob_rawsize(obj)
                           };
            return blob;
        }
    }
}
