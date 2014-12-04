using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Writes data to a <see cref="GitBuf"/> pointer
    /// </summary>
    public class GitBufWriter : BinaryWriter
    {
        private readonly IntPtr gitBufPointer;
        private readonly GitBuf gitBuf;

        internal GitBufWriter(IntPtr gitBufPointer)
        {
            this.gitBufPointer = gitBufPointer;
            this.gitBuf = gitBufPointer.MarshalAs<GitBuf>();
        }

        /// <summary>
        /// Write bytes to the <see cref="GitBuf"/> pointer
        /// </summary>
        /// <param name="bytes">The bytes to write</param>
        public override void Write(byte[] bytes)
        {
            IntPtr reverseBytesPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, reverseBytesPointer, bytes.Length);

            var size = (UIntPtr)bytes.LongLength;
            var allocatedSize = (UIntPtr)bytes.LongLength + 1;
            NativeMethods.git_buf_set(gitBuf, reverseBytesPointer, size);
            gitBuf.size = size;
            gitBuf.asize = allocatedSize;

            Marshal.StructureToPtr(gitBuf, gitBufPointer, true);
        }
    }

    /// <summary>
    /// Reads data from a <see cref="GitBuf"/> pointer
    /// </summary>
    public class GitBufReader : BinaryReader
    {
        internal GitBufReader(IntPtr gitBufPointer) : base(WrapGifBufPointer(gitBufPointer))
        {
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
            var outMessage = new byte[Size];
            Read(outMessage, 0, (int)Size);
            return outMessage;
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
