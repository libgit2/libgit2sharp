using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
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
}