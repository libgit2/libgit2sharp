using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class GitBufWriteStream2 : Stream
    {
        private readonly IntPtr gitBufPointer;
        private GitBuf gitBuf;

        internal GitBufWriteStream2(IntPtr gitBufPointer)
        {
            this.gitBufPointer = gitBufPointer;
            this.gitBuf = gitBufPointer.MarshalAs<GitBuf>();
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            IntPtr bytesPtr = Marshal.AllocHGlobal(count);
            Marshal.Copy(buffer, offset, bytesPtr, count);

            NativeMethods.git_buf_put(gitBuf, bytesPtr, (UIntPtr)count);

            Marshal.FreeHGlobal(bytesPtr);
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }

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
            if (base.CanSeek) // False if stream has already been written/closed
            {
                using (var gitBuf = gitBufPointer.MarshalAs<GitBuf>())
                {
                    WriteTo(gitBuf);
                }
            }

            base.Dispose(disposing);
        }

        private void WriteTo(GitBuf gitBuf)
        {
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
