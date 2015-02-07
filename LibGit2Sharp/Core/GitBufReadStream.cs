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
        private readonly GitBuf gitBuf;

        internal GitBufReadStream(IntPtr gitBufPointer)
            : this(gitBufPointer.MarshalAs<GitBuf>())
        { }

        private unsafe GitBufReadStream(GitBuf gitBuf)
            : base((byte*)gitBuf.ptr,
                   ConvertToLong(gitBuf.size),
                   ConvertToLong(gitBuf.asize),
                   FileAccess.Read)
        {
            this.gitBuf = gitBuf;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && gitBuf != default(GitBuf))
                gitBuf.Dispose();
        }

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
    }
}
