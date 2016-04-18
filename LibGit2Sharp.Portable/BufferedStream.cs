namespace LibGit2Sharp
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A cheap substitute for the .NET BufferedStream class that isn't available on portable profiles.
    /// </summary>
    internal class BufferedStream : Stream
    {
        private int bufferSize;
        private Stream targetStream;
        private MemoryStream bufferStream;

        public BufferedStream(Stream targetStream, int bufferSize)
        {
            Ensure.ArgumentNotNull(targetStream, nameof(targetStream));

            this.targetStream = targetStream;
            this.bufferSize = bufferSize;
            this.bufferStream = new MemoryStream(bufferSize);
        }

        public override bool CanRead => false; // this implementation only supports writing.

        public override bool CanSeek => false;

        public override bool CanWrite => this.targetStream.CanWrite;

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            if (this.bufferStream.Length > 0)
            {
                this.bufferStream.Position = 0;
                this.bufferStream.CopyTo(this.targetStream, this.bufferSize);
                this.bufferStream.Position = 0;
                this.bufferStream.SetLength(0);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.bufferStream.Write(buffer, offset, count);
            if (this.bufferStream.Length > this.bufferSize)
            {
                this.Flush();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Flush();
                this.targetStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
