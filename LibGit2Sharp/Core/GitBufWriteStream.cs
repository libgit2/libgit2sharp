using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class GitBufWriteStream : Stream
    {
        private readonly IntPtr gitBufPointer;
        private GitBuf gitBuf;

        internal GitBufWriteStream(IntPtr gitBufPointer)
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

            Marshal.StructureToPtr(gitBuf, gitBufPointer, true);
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
}
