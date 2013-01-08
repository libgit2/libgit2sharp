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

using System;
using System.IO;
using System.Threading;
using tar_cs;

namespace tar_cs
{
    public class LegacyTarWriter : IDisposable
    {
        private readonly Stream outStream;
        protected byte[] buffer = new byte[1024];
        private bool isClosed;
        public bool ReadOnZero = true;

        /// <summary>
        /// Writes tar (see GNU tar) archive to a stream
        /// </summary>
        /// <param name="writeStream">stream to write archive to</param>
        public LegacyTarWriter(Stream writeStream)
        {
            outStream = writeStream;
        }

        protected virtual Stream OutStream
        {
            get { return outStream; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        public void Write(string fileName)
        {
            using (FileStream file = File.OpenRead(fileName))
            {
                Write(file, file.Length, fileName, 61, 61, 511, File.GetLastWriteTime(file.Name));
            }
        }

        public void Write(FileStream file)
        {
            string path = Path.GetFullPath(file.Name).Replace(Path.GetPathRoot(file.Name),string.Empty);
            path = path.Replace(Path.DirectorySeparatorChar, '/');
            Write(file, file.Length, path, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public void Write(Stream data, long dataSizeInBytes, string name)
        {
            Write(data, dataSizeInBytes, name, 61, 61, 511, DateTime.Now);
        }

        public virtual void Write(string name, long dataSizeInBytes, int userId, int groupId, int mode, DateTime lastModificationTime, WriteDataDelegate writeDelegate)
        {
            IArchiveDataWriter writer = new DataWriter(OutStream, dataSizeInBytes);
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode);
            while(writer.CanWrite)
            {
                writeDelegate(writer);
            }
            AlignTo512(dataSizeInBytes, false);
        }

        public virtual void Write(Stream data, long dataSizeInBytes, string name, int userId, int groupId, int mode,
                                  DateTime lastModificationTime)
        {
            if(isClosed)
                throw new TarException("Can not write to the closed writer");
            WriteHeader(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode);
            WriteContent(dataSizeInBytes, data);
            AlignTo512(dataSizeInBytes,false);
        }

        protected void WriteContent(long count, Stream data)
        {
            while (count > 0 && count > buffer.Length)
            {
                int bytesRead = data.Read(buffer, 0, buffer.Length);
                if (bytesRead < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    if (ReadOnZero)
                        Thread.Sleep(100);
                    else
                        break;
                }
                OutStream.Write(buffer, 0, bytesRead);
                count -= bytesRead;
            }
            if (count > 0)
            {
                int bytesRead = data.Read(buffer, 0, (int) count);
                if (bytesRead < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    while (count > 0)
                    {
                        OutStream.WriteByte(0);
                        --count;
                    }
                }
                else
                    OutStream.Write(buffer, 0, bytesRead);
            }
        }

        protected virtual void WriteHeader(string name, DateTime lastModificationTime, long count, int userId, int groupId, int mode)
        {
            var header = new TarHeader
                         {
                             FileName = name,
                             LastModification = lastModificationTime,
                             SizeInBytes = count,
                             UserId = userId,
                             GroupId = groupId,
                             Mode = mode
                         };
            OutStream.Write(header.GetHeaderValue(), 0, header.HeaderSize);
        }


        public void AlignTo512(long size,bool acceptZero)
        {
            size = size%512;
            if (size == 0 && !acceptZero) return;
            while (size < 512)
            {
                OutStream.WriteByte(0);
                size++;
            }
        }

        public virtual void Close()
        {
            if (isClosed) return;
            AlignTo512(0,true);
            AlignTo512(0,true);
            isClosed = true;
        }
    }
}