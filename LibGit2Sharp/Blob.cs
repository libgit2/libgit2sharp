using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Stores the binary content of a tracked file.
    /// </summary>
    public class Blob : GitObject
    {
        private readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Blob()
        { }

        internal Blob(Repository repo, ObjectId id)
            : base(id)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the size in bytes of the contents of a blob
        /// </summary>
        public virtual int Size { get; set; }

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

        internal static Blob BuildFromPtr(GitObjectSafeHandle obj, ObjectId id, Repository repo)
        {
            var blob = new Blob(repo, id)
                           {
                               Size = Proxy.git_blob_rawsize(obj)
                           };
            return blob;
        }
    }
}
