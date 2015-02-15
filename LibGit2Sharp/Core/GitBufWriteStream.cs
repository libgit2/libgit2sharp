using System;
using System.IO;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class GitBufWriteStream : Stream
    {
        private readonly IntPtr gitBufPointer;

        internal GitBufWriteStream(IntPtr gitBufPointer)
        {
            this.gitBufPointer = gitBufPointer;

            //Preallocate the buffer
            Proxy.git_buf_grow(gitBufPointer, 4096);
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
            AutoGrowBuffer(count);

            Proxy.git_buf_put(gitBufPointer, buffer, offset, count);
        }

        private void AutoGrowBuffer(int count)
        {
            var gitBuf = gitBufPointer.MarshalAs<GitBuf>();

            var ulongCount = Convert.ToUInt64(count);
            var asize = (ulong)gitBuf.asize;
            var size = (ulong)gitBuf.size;

            var isBufferLargeEnoughToHoldTheNewData = (asize - size) > ulongCount;
            var filledBufferPercentage = (100.0 * size / asize);

            if (isBufferLargeEnoughToHoldTheNewData && filledBufferPercentage < 90)
            {
                return;
            }

            var targetSize = (ulong)(1.5 * (asize + ulongCount));

            Proxy.git_buf_grow(gitBufPointer, targetSize);
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
