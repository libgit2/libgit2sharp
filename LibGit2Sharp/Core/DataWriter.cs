/*
 * BSD License
 * 
 * Copyright (c) 2009, Vladimir Vasiltsov
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * 
 * * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * * Names of its contributors may not be used to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.IO;

namespace tar_cs
{
    internal class DataWriter : IArchiveDataWriter
    {
        private readonly long size;
        private long remainingBytes;
        private bool canWrite = true;
        private readonly Stream stream;

        public DataWriter(Stream data, long dataSizeInBytes)
        {
            size = dataSizeInBytes;
            remainingBytes = size;
            stream = data;
        }

        public int Write(byte[] buffer, int count)
        {
            if(remainingBytes == 0)
            {
                canWrite = false;
                return -1;
            }
            int bytesToWrite;
            if(remainingBytes - count < 0)
            {
                bytesToWrite = (int)remainingBytes;
            }
            else
            {
                bytesToWrite = count;
            }
            stream.Write(buffer,0,bytesToWrite);
            remainingBytes -= bytesToWrite;
            return bytesToWrite;
        }

        public bool CanWrite
        {
            get
            {
                return canWrite;
            }
        }
    }
}