﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Stores the binary content of a tracked file.
    /// </summary>
    public class Blob : GitObject, IBlob
    {
        private readonly Repository repo;

        internal Blob(Repository repo, ObjectId id)
            : base(id)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the size in bytes of the contents of a blob
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        ///   Gets the blob content in a <see cref="byte" /> array.
        /// </summary>
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

        /// <summary>
        ///   Gets the blob content in a <see cref="Stream" />.
        /// </summary>
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

        /// <summary>
        ///   Gets the blob content decoded as UTF-8.
        /// </summary>
        /// <returns></returns>
        public string ContentAsUtf8()
        {
            return Encoding.UTF8.GetString(Content);
        }

        /// <summary>
        ///   Gets the blob content decoded as Unicode.
        /// </summary>
        /// <returns></returns>
        public string ContentAsUnicode()
        {
            return Encoding.Unicode.GetString(Content);
        }

        internal static Blob BuildFromPtr(GitObjectSafeHandle obj, ObjectId id, Repository repo)
        {
            var blob = new Blob(repo, id)
                           {
                               Size = NativeMethods.git_blob_rawsize(obj)
                           };
            return blob;
        }
    }
}
