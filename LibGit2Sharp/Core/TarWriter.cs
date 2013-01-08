/*
 * Source: http://code.google.com/p/tar-cs/
 * 
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
using System.Linq;
using System.Net;
using System.Text;

namespace LibGit2Sharp.Core
{
    internal class TarWriter : IDisposable
    {
        private readonly Stream outStream;

        /// <summary>
        /// Writes tar (see GNU tar) archive to a stream
        /// </summary>
        /// <param name="writeStream">stream to write archive to</param>
        public TarWriter(Stream writeStream)
        {
            outStream = writeStream;
        }

        protected Stream OutStream
        {
            get { return outStream; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            AlignTo512(0, true);
            AlignTo512(0, true);

            GC.SuppressFinalize(this);
        }

        #endregion

        public void Write(
            string fileName,
            Stream data,
            DateTimeOffset modificationTime,
            int mode,
            string userId,
            string groupId,
            char typeflag,
            string userName,
            string groupName,
            string deviceMajorNumber,
            string deviceMinorNumber)
        {
            WriteHeader(fileName, modificationTime, (data != null)?data.Length:0, mode, userId, groupId, typeflag, userName, groupName, deviceMajorNumber, deviceMinorNumber);
            // folders have no data
            if (data != null)
            {
                WriteContent(data.Length, data);
            }
            AlignTo512(data.Length, false);
        }

        protected void WriteContent(long count, Stream data)
        {
            var buffer = new byte[1024];

            while (count > 0 && count > buffer.Length)
            {
                int bytesRead = data.Read(buffer, 0, buffer.Length);
                if (bytesRead < 0)
                    throw new IOException("TarWriter unable to read from provided stream");

                OutStream.Write(buffer, 0, bytesRead);
                count -= bytesRead;
            }
            if (count > 0)
            {
                int bytesRead = data.Read(buffer, 0, (int)count);
                if (bytesRead < 0)
                    throw new IOException("TarWriter unable to read from provided stream");
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

        protected void AlignTo512(long size, bool acceptZero)
        {
            size = size % 512;
            if (size == 0 && !acceptZero) return;
            while (size < 512)
            {
                OutStream.WriteByte(0);
                size++;
            }
        }

        protected void WriteHeader(
            string name,
            DateTimeOffset lastModificationTime,
            long count,
            int mode,
            string userId,
            string groupId,
            char typeflag,
            string userName,
            string groupName,
            string deviceMajorNumber,
            string deviceMinorNumber)
        {
            var tarHeader = new UsTarHeader(name, lastModificationTime, count, mode, userId, groupId, typeflag, userName, groupName, deviceMajorNumber, deviceMinorNumber);
            var header = tarHeader.GetHeaderValue();
            OutStream.Write(header, 0, header.Length);
        }

        /// <summary>
        /// UsTar header implementation.
        /// </summary>
        private class UsTarHeader
        {
            private readonly string mode;
            private readonly long size;
            private readonly string unixTime;
            private const string magic = "ustar";
            private const string version = "00";
            private readonly string userName;
            private readonly string groupName;
            private readonly string userId;
            private readonly string groupId;
            private readonly char typeflag;
            private readonly string deviceMajorNumber;
            private readonly string deviceMinorNumber;
            private string namePrefix;
            private string fileName;


            public UsTarHeader(
                string filePath,
                DateTimeOffset lastModificationTime,
                long size,
                int mode,
                string userId,
                string groupId,
                char typeflag,
                string userName,
                string groupName,
                string deviceMajorNumber,
                string deviceMinorNumber)
            {
                #region Length validations

                if (userName.Length > 32)
                {
                    throw new ArgumentException("ustar userName cannot be longer than 32 characters.", "userName");
                }
                if (groupName.Length > 32)
                {
                    throw new ArgumentException("ustar groupName cannot be longer than 32 characters.", "groupName");
                }
                if (userId.Length > 7)
                {
                    throw new ArgumentException("ustar userId cannot be longer than 7 characters.", "userId");
                }
                if (groupId.Length > 7)
                {
                    throw new ArgumentException("ustar groupId cannot be longer than 7 characters.", "groupId");
                }
                if (deviceMajorNumber.Length > 7)
                {
                    throw new ArgumentException("ustar deviceMajorNumber cannot be longer than 7 characters.", "deviceMajorNumber");
                }
                if (deviceMinorNumber.Length > 7)
                {
                    throw new ArgumentException("ustar deviceMinorNumber cannot be longer than 7 characters.", "deviceMinorNumber");
                }

                #endregion

                this.mode = Convert.ToString(mode, 8).PadLeft(7, '0');
                this.size = size;
                unixTime = Convert.ToString(lastModificationTime.ToSecondsSinceEpoch(), 8).PadLeft(11, '0');
                this.userId = userId.PadLeft(7, '0');
                this.groupId = userId.PadLeft(7, '0');
                this.userName = userName;
                this.groupName = groupName;
                this.typeflag = typeflag;
                this.deviceMajorNumber = deviceMajorNumber.PadLeft(7, '0');
                this.deviceMinorNumber = deviceMinorNumber.PadLeft(7, '0');
                
                ParseFileName(filePath);
            }

            private void ParseFileName(string fullFilePath)
            {
                var posixPath = fullFilePath.Replace('/', Path.DirectorySeparatorChar);

                if (posixPath.Length > 100)
                {
                    if (posixPath.Length > 255)
                    {
                        throw new ArgumentException(string.Format("ustar filePath ({0}) cannot be longer than 255 chars", posixPath));
                    }
                    int position = posixPath.Length - 100;

                    // Find first path separator in the remaining 100 chars of the file name
                    while (!Equals('/', posixPath[position]))
                    {
                        ++position;
                        if (position == posixPath.Length)
                        {
                            break;
                        }
                    }
                    if (position == posixPath.Length)
                        position = posixPath.Length - 100;
                    namePrefix = posixPath.Substring(0, position);
                    fileName = posixPath.Substring(position, posixPath.Length - position);
                }
                else
                {
                    namePrefix = string.Empty;
                    fileName = posixPath;
                }
            }

            public byte[] GetHeaderValue()
            {
                var buffer = new byte[512];

                // Fill header
                Encoding.ASCII.GetBytes(fileName.PadRight(100, '\0')).CopyTo(buffer, 0);
                Encoding.ASCII.GetBytes(mode).CopyTo(buffer, 100);
                Encoding.ASCII.GetBytes(userId).CopyTo(buffer, 108);
                Encoding.ASCII.GetBytes(groupId).CopyTo(buffer, 116);
                Encoding.ASCII.GetBytes(Convert.ToString(size, 8).PadLeft(11, '0')).CopyTo(buffer, 124);
                Encoding.ASCII.GetBytes(unixTime).CopyTo(buffer, 136);
                buffer[156] = Convert.ToByte(typeflag);

                Encoding.ASCII.GetBytes(magic).CopyTo(buffer, 257); // Mark header as ustar
                Encoding.ASCII.GetBytes(version).CopyTo(buffer, 263);
                Encoding.ASCII.GetBytes(userName).CopyTo(buffer, 265);
                Encoding.ASCII.GetBytes(groupName).CopyTo(buffer, 297);
                Encoding.ASCII.GetBytes(deviceMajorNumber).CopyTo(buffer, 329);
                Encoding.ASCII.GetBytes(deviceMinorNumber).CopyTo(buffer, 337);
                Encoding.ASCII.GetBytes(namePrefix).CopyTo(buffer, 345);

                if (size >= 0x1FFFFFFFF)
                {
                    byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(size));
                    SetMarker(AlignTo12(bytes)).CopyTo(buffer, 124);
                }

                string checksum = CalculateChecksum(buffer);
                Encoding.ASCII.GetBytes(checksum).CopyTo(buffer, 148);
                Encoding.ASCII.GetBytes("\0").CopyTo(buffer, 155);

                return buffer;
            }

            private string CalculateChecksum(byte[] buf)
            {
                Encoding.ASCII.GetBytes(new string(' ', 8)).CopyTo(buf, 148);

                long headerChecksum = buf.Aggregate<byte, long>(0, (current, b) => current + b);

                return Convert.ToString(headerChecksum, 8).PadLeft(7, '0');
            }

            private static byte[] SetMarker(byte[] bytes)
            {
                bytes[0] |= 0x80;
                return bytes;
            }

            private static byte[] AlignTo12(byte[] bytes)
            {
                var retVal = new byte[12];
                bytes.CopyTo(retVal, 12 - bytes.Length);
                return retVal;
            }
        }
    }
}
