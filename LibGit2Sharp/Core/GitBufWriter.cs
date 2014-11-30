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
    public class GitBufWriter
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
        public void Write(byte[] bytes)
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
    public class GitBufReader
    {
        private GitBuf gitBuf;

        internal GitBufReader(IntPtr gitBufPointer)
        {
            this.gitBuf = gitBufPointer.MarshalAs<GitBuf>();
        }

        /// <summary>
        /// Reads all bytes from the <see cref="GitBuf"/>
        /// </summary>
        /// <returns></returns>
        public unsafe byte[] Read()
        {
            // Get a byte pointer from the IntPtr object. 
            byte* memBytePtr = (byte*)gitBuf.ptr;
            long size = ConvertToLong(gitBuf.size);
            long length = ConvertToLong(gitBuf.asize);

            // Create another UnmanagedMemoryStream object using a pointer to unmanaged memory.
            var readStream = new UnmanagedMemoryStream(memBytePtr, size, length, FileAccess.Read);

            // Create a byte array to hold data from unmanaged memory. 
            byte[] outMessage = new byte[size];

            // Read from unmanaged memory to the byte array.
            readStream.Read(outMessage, 0, (int)size);

            // Close the stream.
            readStream.Close();
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

    }

}
