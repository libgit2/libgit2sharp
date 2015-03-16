using System.IO;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="Blob"/>.
    /// </summary>
    public static class BlobExtensions
    {
        /// <summary>
        /// Gets the blob content decoded with the specified encoding,
        /// or according to byte order marks, with UTF8 as fallback,
        /// if <paramref name="encoding"/> is null.
        /// </summary>
        /// <param name="blob">The blob for which the content will be returned.</param>
        /// <param name="encoding">The encoding of the text. (default: detected or UTF8)</param>
        /// <returns>Blob content as text.</returns>
        public static string GetContentText(this Blob blob, Encoding encoding = null)
        {
            Ensure.ArgumentNotNull(blob, "blob");

            return ReadToEnd(blob.GetContentStream(), encoding);
        }

        /// <summary>
        /// Gets the blob content as it would be checked out to the
        /// working directory, decoded with the specified encoding,
        /// or according to byte order marks, with UTF8 as fallback,
        /// if <paramref name="encoding"/> is null.
        /// </summary>
        /// <param name="blob">The blob for which the content will be returned.</param>
        /// <param name="filteringOptions">Parameter controlling content filtering behavior</param>
        /// <param name="encoding">The encoding of the text. (default: detected or UTF8)</param>
        /// <returns>Blob content as text.</returns>
        public static string GetContentText(this Blob blob, FilteringOptions filteringOptions, Encoding encoding = null)
        {
            Ensure.ArgumentNotNull(blob, "blob");
            Ensure.ArgumentNotNull(filteringOptions, "filteringOptions");

            return ReadToEnd(blob.GetContentStream(filteringOptions), encoding);
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
