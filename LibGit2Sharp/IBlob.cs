using System.IO;

namespace LibGit2Sharp
{
    public interface IBlob : IGitObject
    {
        /// <summary>
        ///   Gets the size in bytes of the contents of a blob
        /// </summary>
        int Size { get; set; }

        byte[] Content { get; }
        Stream ContentStream { get; }
    }
}