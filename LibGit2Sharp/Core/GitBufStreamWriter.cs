using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Writes data to a <see cref="GitBuf"/> pointer
    /// </summary>
    public class GitBufStreamWriter
    {
        private readonly IntPtr gitBufPointer;
        private readonly GitBuf gitBuf;

        public GitBufStreamWriter(IntPtr gitBufPointer)
        {
            this.gitBufPointer = gitBufPointer;
            this.gitBuf = gitBufPointer.MarshalAs<GitBuf>();
        }

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