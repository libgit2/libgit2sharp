using System;
using System.IO;

namespace LibGit2Sharp.Core
{
    class WriteStream : Stream
    {
        readonly GitWriteStream nextStream;
        readonly IntPtr nextPtr;

        public WriteStream(GitWriteStream nextStream, IntPtr nextPtr)
        {
            this.nextStream = nextStream;
            this.nextPtr = nextPtr;
        }

        public override bool CanWrite { get { return true; } }

        public override bool CanRead { get { return false; } }

        public override bool CanSeek { get { return false; } }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new InvalidOperationException(); }
        }

        public override long Length { get { throw new InvalidOperationException(); } }

        public override void Flush()
        { }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int res;
            unsafe
            {
                fixed (byte* bufferPtr = &buffer[offset])
                {
                    res = nextStream.write(nextPtr, (IntPtr)bufferPtr, (UIntPtr)count);
                }
            }

            Ensure.Int32Result(res);
        }
    }
}
