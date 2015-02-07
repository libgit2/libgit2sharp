using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Writes data to a <see cref="GitBuf"/> pointer
    /// </summary>
    internal class GitBufWriteStream : MemoryStream
    {
        private readonly IntPtr gitBufPointer;

        internal GitBufWriteStream(IntPtr gitBufPointer)
        {
            this.gitBufPointer = gitBufPointer;
        }

        protected override void Dispose(bool disposing)
        {
            using (var gitBuf = gitBufPointer.MarshalAs<GitBuf>())
                WriteTo(gitBuf);

            base.Dispose(disposing);
        }

        private void WriteTo(GitBuf gitBuf)
        {
            if (!base.CanSeek)
            {
                // Already closed; already written
                return;
            }

            Seek(0, SeekOrigin.Begin);

            var length = (int)Length;
            var bytes = new byte[length];
            Read(bytes, 0, length);

            IntPtr reverseBytesPointer = Marshal.AllocHGlobal(length);
            Marshal.Copy(bytes, 0, reverseBytesPointer, bytes.Length);

            var size = (UIntPtr)length;
            var allocatedSize = (UIntPtr)length;
            NativeMethods.git_buf_set(gitBuf, reverseBytesPointer, size);
            gitBuf.size = size;
            gitBuf.asize = allocatedSize;

            Marshal.StructureToPtr(gitBuf, gitBufPointer, true);
        }
    }
}
