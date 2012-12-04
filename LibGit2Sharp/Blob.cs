using System;
using System.IO;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Stores the binary content of a tracked file.
    /// </summary>
    public class Blob : GitObject
    {
        private readonly ILazy<Int64> lazySize;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Blob()
        { }

        internal Blob(Repository repo, ObjectId id)
            : base(repo, id)
        {
            lazySize = GitObjectLazyGroup.Singleton(repo, id, Proxy.git_blob_rawsize);
        }

        /// <summary>
        ///   Gets the size in bytes of the contents of a blob
        /// </summary>
        public virtual int Size { get { return (int)lazySize.Value; } }

        /// <summary>
        ///   Gets the blob content in a <see cref="byte" /> array.
        /// </summary>
        public virtual byte[] Content
        {
            get
            {
                return Proxy.git_blob_rawcontent(repo.Handle, Id, Size);
            }
        }

        /// <summary>
        ///   Gets the blob content in a <see cref="Stream" />.
        /// </summary>
        public virtual Stream ContentStream
        {
            get
            {
                return Proxy.git_blob_rawcontent_stream(repo.Handle, Id, Size);
            }
        }
    }
}
