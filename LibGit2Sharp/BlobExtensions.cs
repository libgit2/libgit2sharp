using System.Text;

namespace LibGit2Sharp
{
    public static class BlobExtensions
    {
        public static string ContentAsUtf8(this IBlob blob)
        {
            return Encoding.UTF8.GetString(blob.Content);
        }

        public static string ContentAsUnicode(this IBlob blob)
        {
            return Encoding.Unicode.GetString(blob.Content);
        }
    }
}