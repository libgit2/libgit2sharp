using System;
using System.IO;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Stores the binary content of a tracked file.
    /// </summary>
    public class Blob : GitObject
    {
        private readonly ILazy<Int64> lazySize;
        private readonly ILazy<bool> lazyIsBinary;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Blob()
        { }

        internal Blob(Repository repo, ObjectId id)
            : base(repo, id)
        {
            lazySize = GitObjectLazyGroup.Singleton(repo, id, Proxy.git_blob_rawsize);
            lazyIsBinary = GitObjectLazyGroup.Singleton(repo, id, Proxy.git_blob_is_binary);
        }

        /// <summary>
        /// Gets the size in bytes of the raw content of a blob.
        /// </summary>
        public virtual int Size { get { return (int)lazySize.Value; } }

        /// <summary>
        ///  Determine if the blob content is most certainly binary or not.
        /// </summary>
        public virtual bool IsBinary { get { return lazyIsBinary.Value; } }

        /// <summary>
        /// Gets the blob content in a <see cref="byte"/> array.
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use one of the GetContentStream() overloads instead.")]
        public virtual byte[] Content
        {
            get
            {
                return Proxy.git_blob_rawcontent(repo.Handle, Id, Size);
            }
        }

        /// <summary>
        /// Gets the blob content in a <see cref="Stream"/>.
        /// </summary>
        public virtual Stream GetContentStream()
        {
            return Proxy.git_blob_rawcontent_stream(repo.Handle, Id, Size);
        }

        /// <summary>
        /// Gets the blob content in a <see cref="Stream"/> as it would be
        /// checked out to the working directory.
        /// <param name="filteringOptions">Parameter controlling content filtering behavior</param>
        /// </summary>
        public virtual Stream GetContentStream(FilteringOptions filteringOptions)
        {
            Ensure.ArgumentNotNull(filteringOptions, "filteringOptions");
            return Proxy.git_blob_filtered_content_stream(repo.Handle, Id, filteringOptions.HintPath, false);
        }

        /// <summary>
        /// Gets the blob content in a <see cref="Stream"/>.
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use one of the GetContentStream() overloads instead.")]
        public virtual Stream ContentStream
        {
            get
            {
                return GetContentStream();
            }
        }
    }
}
