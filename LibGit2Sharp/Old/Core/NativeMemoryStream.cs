/*
 * The MIT License
 *
 * Copyright (c) 2011 Andrius Bentkus
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.IO;

namespace LibGit2Sharp.Core
{
    unsafe public class NativeMemoryStream : Stream
    {
        void *ptr;

        private long length;
        private long position = 0;

        internal NativeMemoryStream(IntPtr ptr, long length)
            : this(ptr.ToPointer(), length)
        {
        }

        internal NativeMemoryStream(void *ptr, long length)
        {
            this.ptr = ptr;
            this.length = length;
        }

        #region implemented abstract members of System.IO.Stream

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            for (int i = 0; i < count; i++)
            {
                if (position >= Length)
                    continue;

                buffer[i + offset] = ((byte *)ptr)[position];
                position++;
                read++;
            }
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = (long)ptr + offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position = (long)ptr + length + offset;
                    break;
            }
            return position;
        }

        public override bool CanRead {
            get {
                return true;
            }
        }

        public override bool CanSeek {
            get {
                return true;
            }
        }

        public override bool CanWrite {
            get {
                return false;
            }
        }

        public override long Length {
            get {
                return length;
            }
        }

        public override long Position {
            get {
                return position;
            }
            set {
                if ((long)ptr + position >= length)
                    throw new ArgumentException();

                position = value;
            }
        }

        #endregion
    }
}
