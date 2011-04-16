using System;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Blob : GitObject
    {
        private readonly Repository _repo;

        internal Blob(Repository repo, ObjectId id)
            : base(id)
        {
            _repo = repo;
        }

        public int Size { get; set; }

        public byte[] Content
        {
            get
            {
                IntPtr obj;
                var oid = Id.Oid;
                var res = NativeMethods.git_object_lookup(out obj, _repo.Handle, ref oid, GitObjectType.Blob);
                Ensure.Success(res);
                try
                {
                    var ptr = NativeMethods.git_blob_rawcontent(obj);
                    var arr = new byte[Size];
                    Marshal.Copy(ptr, arr, 0, Size);
                    return arr;
                }
                finally
                {
                    NativeMethods.git_object_close(obj);
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