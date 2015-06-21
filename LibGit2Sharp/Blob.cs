using System;
using System.IO;
using System.Text;
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
        /// <para> Please note that this will load the entire blob content in the memory to compute the Size.
        /// In order to only read the blob size from header, <see cref="ObjectDatabase.RetrieveObjectMetadata"/>
        /// can be used.
        /// </para>
        /// </summary>
        public virtual long Size { get { return lazySize.Value; } }

        /// <summary>
        ///  Determine if the blob content is most certainly binary or not.
        /// </summary>
        public virtual bool IsBinary { get { return lazyIsBinary.Value; } }

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
        /// Gets the blob content, decoded with UTF8 encoding if the encoding cannot be detected from the byte order mark.
        /// </summary>
        /// <returns>Blob content as text.</returns>
        public virtual string GetContentText()
        {
            return ReadToEnd(GetContentStream(), null);
        }

        /// <summary>
        /// Gets the blob content decoded with the specified encoding.
        /// </summary>
        /// <param name="encoding">The encoding of the text to use, if it cannot be detected</param>
        /// <returns>Blob content as text.</returns>
        public virtual string GetContentText(Encoding encoding)
        {
            Ensure.ArgumentNotNull(encoding, "encoding");

            return ReadToEnd(GetContentStream(), encoding);
        }

        /// <summary>
        /// Gets the blob content, decoded with UTF8 encoding if the encoding cannot be detected from the byte order mark.
        /// </summary>
        /// <param name="filteringOptions">Parameter controlling content filtering behavior</param>
        /// <returns>Blob content as text.</returns>
        public virtual string GetContentText(FilteringOptions filteringOptions)
        {
            Ensure.ArgumentNotNull(filteringOptions, "filteringOptions");

            return ReadToEnd(GetContentStream(filteringOptions), null);
        }

        /// <summary>
        /// Gets the blob content as it would be checked out to the
        /// working directory, decoded with the specified encoding.
        /// </summary>
        /// <param name="filteringOptions">Parameter controlling content filtering behavior</param>
        /// <param name="encoding">The encoding of the text.</param>
        /// <returns>Blob content as text.</returns>
        public virtual string GetContentText(FilteringOptions filteringOptions, Encoding encoding)
        {
            Ensure.ArgumentNotNull(filteringOptions, "filteringOptions");
            Ensure.ArgumentNotNull(encoding, "encoding");

            return ReadToEnd(GetContentStream(filteringOptions), encoding);
        }

        private static string ReadToEnd(Stream stream, Encoding encoding)
        {
            using (var reader = new StreamReader(stream, encoding ?? LaxUtf8Marshaler.Encoding, encoding == null))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
