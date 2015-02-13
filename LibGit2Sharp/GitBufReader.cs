using System;
using System.Globalization;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Reads data from a <see cref="GitBuf"/> pointer
    /// </summary>
    public class GitBufReader
    {
        private readonly IntPtr gitBufPointer;

        internal GitBufReader(IntPtr gitBufPointer)
        {
            this.gitBufPointer = gitBufPointer;
            var gitBuf = gitBufPointer.MarshalAs<GitBuf>();
            Size = ConvertToLong(gitBuf.size);
            Allocated = ConvertToLong(gitBuf.asize);
        }

        /// <summary>
        /// The size of the underlying stream
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// The allocated size of the underlying stream
        /// </summary>
        public long Allocated { get; private set; }

        /// <summary>
        /// Reads all bytes from the <see cref="GitBuf"/>
        /// </summary>
        /// <returns></returns>
        public byte[] ReadAll()
        {
            using (var unmanagedStream = WrapGifBufPointer(gitBufPointer))
            {
                var outMessage = new byte[Size];
                unmanagedStream.Read(outMessage, 0, (int)Size);
                return outMessage;
            }
        }

        internal static long ConvertToLong(UIntPtr len)
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

        private unsafe static Stream WrapGifBufPointer(IntPtr gitBufPointer)
        {
            var gitBuf = gitBufPointer.MarshalAs<GitBuf>();
            byte* memBytePtr = (byte*)gitBuf.ptr;
            long size = ConvertToLong(gitBuf.size);
            long length = ConvertToLong(gitBuf.asize);

            return new UnmanagedMemoryStream(memBytePtr, size, length, FileAccess.Read);
        }
    }
}
