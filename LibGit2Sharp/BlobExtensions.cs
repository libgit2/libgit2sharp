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
        /// Gets the blob content decoded as UTF-8.
        /// </summary>
        /// <param name="blob">The blob for which the content will be returned.</param>
        /// <returns>Blob content as UTF-8</returns>
        public static string ContentAsUtf8(this Blob blob)
        {
            Ensure.ArgumentNotNull(blob, "blob");

            return Encoding.UTF8.GetString(blob.Content);
        }

        /// <summary>
        /// Gets the blob content decoded as Unicode.
        /// </summary>
        /// <param name="blob">The blob for which the content will be returned.</param>
        /// <returns>Blob content as unicode.</returns>
        public static string ContentAsUnicode(this Blob blob)
        {
            Ensure.ArgumentNotNull(blob, "blob");

            return Encoding.Unicode.GetString(blob.Content);
        }
    }
}
