using System;
using System.Globalization;
using System.IO;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Reads data from a <see cref="GitBuf"/> pointer
    /// </summary>
    internal class GitBufReadStream : UnmanagedMemoryStream
    {
        internal GitBufReadStream(IntPtr gitBufPointer)
            : this(gitBufPointer.MarshalAs<GitBuf>())
        { }

        private unsafe GitBufReadStream(GitBuf gitBuf)
            : base((byte*)gitBuf.ptr,
                   ConvertToLong(gitBuf.size),
                   ConvertToLong(gitBuf.asize),
                   FileAccess.Read)
        { }

        private static long ConvertToLong(UIntPtr len)
        {
            if (len.ToUInt64() > long.MaxValue)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Provided length ({0}) exceeds long.MaxValue ({1}).",
                        len.ToUInt64(), long.MaxValue));
            }

            return (long)len.ToUInt64();
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            throw new NotSupportedException();
        }

        public override bool CanSeek
        {
            get { return false; }
        }
    }
}
