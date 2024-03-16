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
using System.Globalization;
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
            FilePath filePath,
            Stream data,
            DateTimeOffset modificationTime,
            int mode,
            string userId,
            string groupId,
            char typeflag,
            string userName,
            string groupName,
            string deviceMajorNumber,
            string deviceMinorNumber,
            string entrySha,
            bool isLink)
        {
            FileNameExtendedHeader fileNameExtendedHeader = FileNameExtendedHeader.Parse(filePath.Posix, entrySha);
            LinkExtendedHeader linkExtendedHeader = ParseLink(isLink, data, entrySha);

            WriteExtendedHeader(fileNameExtendedHeader, linkExtendedHeader, entrySha, modificationTime);

            // Note: in case of links, we won't add a content, but the size in the header will still be != 0. It seems strange, but it seem to be what git.git is doing?
            WriteHeader(fileNameExtendedHeader.Name,
                        fileNameExtendedHeader.Prefix,
                        modificationTime,
                        (data != null)
                            ? data.Length
                            : 0,
                        mode,
                        userId,
                        groupId,
                        typeflag,
                        linkExtendedHeader.Link,
                        userName,
                        groupName,
                        deviceMajorNumber,
                        deviceMinorNumber);

            // folders have no data, and so do links
            if (data != null && !isLink)
            {
                WriteContent(data.Length, data, OutStream);
            }
            AlignTo512((data != null) ? data.Length : 0, false);
        }

        protected static void WriteContent(long count, Stream data, Stream dest)
        {
            var buffer = new byte[1024];

            while (count > 0 && count > buffer.Length)
            {
                int bytesRead = data.Read(buffer, 0, buffer.Length);
                if (bytesRead < 0)
                {
                    throw new IOException("TarWriter unable to read from provided stream");
                }

                dest.Write(buffer, 0, bytesRead);
                count -= bytesRead;
            }
            if (count > 0)
            {
                int bytesRead = data.Read(buffer, 0, (int)count);
                if (bytesRead < 0)
                {
                    throw new IOException("TarWriter unable to read from provided stream");
                }

                if (bytesRead == 0)
                {
                    while (count > 0)
                    {
                        dest.WriteByte(0);
                        --count;
                    }
                }
                else
                {
                    dest.Write(buffer, 0, bytesRead);
                }
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
            string fileName,
            string namePrefix,
            DateTimeOffset lastModificationTime,
            long count,
            int mode,
            string userId,
            string groupId,
            char typeflag,
            string link,
            string userName,
            string groupName,
            string deviceMajorNumber,
            string deviceMinorNumber)
        {
            var tarHeader = new UsTarHeader(fileName,
                                            namePrefix,
                                            lastModificationTime,
                                            count,
                                            mode,
                                            userId,
                                            groupId,
                                            typeflag,
                                            link,
                                            userName,
                                            groupName,
                                            deviceMajorNumber,
                                            deviceMinorNumber);
            var header = tarHeader.GetHeaderValue();
            OutStream.Write(header, 0, header.Length);
        }

        private static LinkExtendedHeader ParseLink(bool isLink, Stream data, string entrySha)
        {
            if (!isLink)
            {
                return new LinkExtendedHeader(string.Empty, string.Empty, false);
            }

            using (var dest = new MemoryStream())
            {
                WriteContent(data.Length, data, dest);
                dest.Seek(0, SeekOrigin.Begin);

                using (var linkStream = new StreamReader(dest))
                {
                    string link = linkStream.ReadToEnd();

                    if (data.Length > 100)
                    {
                        return new LinkExtendedHeader(link,
                                                      string.Format(CultureInfo.InvariantCulture,
                                                                    "see %s.paxheader{0}",
                                                                    entrySha),
                                                      true);
                    }

                    return new LinkExtendedHeader(link, link, false);
                }
            }
        }

        private void WriteExtendedHeader(FileNameExtendedHeader fileNameExtendedHeader, LinkExtendedHeader linkExtendedHeader, string entrySha,
            DateTimeOffset modificationTime)
        {
            string extHeader = string.Empty;

            if (fileNameExtendedHeader.NeedsExtendedHeaderEntry)
            {
                extHeader += BuildKeyValueExtHeader("path", fileNameExtendedHeader.InitialPath);
            }

            if (linkExtendedHeader.NeedsExtendedHeaderEntry)
            {
                extHeader += BuildKeyValueExtHeader("linkpath", linkExtendedHeader.InitialLink);
            }

            if (string.IsNullOrEmpty(extHeader))
            {
                return;
            }

            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(extHeader)))
            {
                Write(string.Format(CultureInfo.InvariantCulture,
                                    "{0}.paxheader",
                                    entrySha),
                      stream, modificationTime,
                      "666".OctalToInt32(),
                      "0",
                      "0",
                      'x',
                      "root",
                      "root",
                      "0",
                      "0",
                      entrySha,
                      false);
            }
        }

        private static string BuildKeyValueExtHeader(string key, string value)
        {
            // "%u %s=%s\n"
            int len = key.Length + value.Length + 3;
            for (int i = len; i > 9; i /= 10)
            {
                len++;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} {1}={2}\n", len, key, value);
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
            private readonly string link;
            private readonly string deviceMajorNumber;
            private readonly string deviceMinorNumber;
            private readonly string namePrefix;
            private readonly string fileName;

            public UsTarHeader(
                string fileName,
                string namePrefix,
                DateTimeOffset lastModificationTime,
                long size,
                int mode,
                string userId,
                string groupId,
                char typeflag,
                string link,
                string userName,
                string groupName,
                string deviceMajorNumber,
                string deviceMinorNumber)
            {
                #region Length validations

                if (userName.Length > 32)
                {
                    throw new ArgumentException("ustar userName cannot be longer than 32 characters.", nameof(userName));
                }
                if (groupName.Length > 32)
                {
                    throw new ArgumentException("ustar groupName cannot be longer than 32 characters.", nameof(groupName));
                }
                if (userId.Length > 7)
                {
                    throw new ArgumentException("ustar userId cannot be longer than 7 characters.", nameof(userId));
                }
                if (groupId.Length > 7)
                {
                    throw new ArgumentException("ustar groupId cannot be longer than 7 characters.", nameof(groupId));
                }
                if (deviceMajorNumber.Length > 7)
                {
                    throw new ArgumentException("ustar deviceMajorNumber cannot be longer than 7 characters.", nameof(deviceMajorNumber));
                }
                if (deviceMinorNumber.Length > 7)
                {
                    throw new ArgumentException("ustar deviceMinorNumber cannot be longer than 7 characters.", nameof(deviceMinorNumber));
                }
                if (link.Length > 100)
                {
                    throw new ArgumentException("ustar link cannot be longer than 100 characters.", nameof(link));
                }

                #endregion

                this.mode = Convert.ToString(mode, 8).PadLeft(7, '0');
                this.size = size;
                unixTime = Convert.ToString(lastModificationTime.ToUnixTimeSeconds(), 8).PadLeft(11, '0');
                this.userId = userId.PadLeft(7, '0');
                this.groupId = userId.PadLeft(7, '0');
                this.userName = userName;
                this.groupName = groupName;
                this.typeflag = typeflag;
                this.link = link;
                this.deviceMajorNumber = deviceMajorNumber.PadLeft(7, '0');
                this.deviceMinorNumber = deviceMinorNumber.PadLeft(7, '0');

                this.fileName = fileName;
                this.namePrefix = namePrefix;
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
                Encoding.ASCII.GetBytes(link).CopyTo(buffer, 157);

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

            private static string CalculateChecksum(byte[] buf)
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

        private class FileNameExtendedHeader
        {
            private readonly string prefix;
            private readonly string name;
            private readonly string initialPath;
            private readonly bool needsExtendedHeaderEntry;

            private FileNameExtendedHeader(string initialPath, string prefix, string name, bool needsExtendedHeaderEntry)
            {
                this.initialPath = initialPath;
                this.prefix = prefix;
                this.name = name;
                this.needsExtendedHeaderEntry = needsExtendedHeaderEntry;
            }

            public bool NeedsExtendedHeaderEntry
            {
                get { return needsExtendedHeaderEntry; }
            }

            public string Name
            {
                get { return name; }
            }

            public string Prefix
            {
                get { return prefix; }
            }

            public string InitialPath
            {
                get { return initialPath; }
            }

            /// <summary>
            ///   Logic taken from https://github.com/git/git/blob/master/archive-tar.c
            /// </summary>
            public static FileNameExtendedHeader Parse(string posixPath, string entrySha)
            {
                if (posixPath.Length > 100)
                {
                    // Need to increment by one because while loop decrements first before testing for path separator
                    int position = Math.Min(156, posixPath.Length);

                    while (--position > 0 && !Equals('/', posixPath[position]))
                    { }

                    int remaining = posixPath.Length - position - 1;
                    if (remaining < 100 && position > 0)
                    {
                        return new FileNameExtendedHeader(posixPath, posixPath.Substring(0, position), posixPath.Substring(position, posixPath.Length - position), false);
                    }

                    return new FileNameExtendedHeader(posixPath,
                                                      string.Empty,
                                                      string.Format(CultureInfo.InvariantCulture,
                                                                    "{0}.data",
                                                                    entrySha),
                                                      true);
                }

                return new FileNameExtendedHeader(posixPath, string.Empty, posixPath, false);
            }
        }

        private class LinkExtendedHeader
        {
            private readonly string initialLink;
            private readonly string link;
            private readonly bool needsExtendedHeaderEntry;

            public LinkExtendedHeader(string initialLink, string link, bool needsExtendedHeaderEntry)
            {
                this.initialLink = initialLink;
                this.link = link;
                this.needsExtendedHeaderEntry = needsExtendedHeaderEntry;
            }

            public string InitialLink
            {
                get { return initialLink; }
            }

            public bool NeedsExtendedHeaderEntry
            {
                get { return needsExtendedHeaderEntry; }
            }

            public string Link
            {
                get { return link; }
            }
        }
    }
}
