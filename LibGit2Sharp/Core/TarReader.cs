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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace tar_cs
{
    /// <summary>
    /// Extract contents of a tar file represented by a stream for the TarReader constructor
    /// </summary>
    public class TarReader
    {
        private readonly byte[] dataBuffer = new byte[512];
        private readonly UsTarHeader header;
        private readonly Stream inStream;
        private long remainingBytesInFile;

        /// <summary>
        /// Constructs TarReader object to read data from `tarredData` stream
        /// </summary>
        /// <param name="tarredData">A stream to read tar archive from</param>
        public TarReader(Stream tarredData)
        {
            inStream = tarredData;
            header = new UsTarHeader();
        }

        public ITarHeader FileInfo
        {
            get { return header; }
        }

        /// <summary>
        /// Read all files from an archive to a directory. It creates some child directories to
        /// reproduce a file structure from the archive.
        /// </summary>
        /// <param name="destDirectory">The out directory.</param>
        /// 
        /// CAUTION! This method is not safe. It's not tar-bomb proof. 
        /// {see http://en.wikipedia.org/wiki/Tar_(file_format) }
        /// If you are not sure about the source of an archive you extracting,
        /// then use MoveNext and Read and handle paths like ".." and "../.." according
        /// to your business logic.
        public void ReadToEnd(string destDirectory)
        {
            while (MoveNext(false))
            {
                string totalPath = destDirectory + Path.DirectorySeparatorChar + FileInfo.FileName;
                string fileName = Path.GetFileName(totalPath);
                string directory = totalPath.Remove(totalPath.Length - fileName.Length);
                Directory.CreateDirectory(directory);
                using (FileStream file = File.Create(totalPath))
                {
                    Read(file);
                }
            }
        }
        
        /// <summary>
        /// Read data from a current file to a Stream.
        /// </summary>
        /// <param name="dataDestanation">A stream to read data to</param>
        /// 
        /// <seealso cref="MoveNext"/>
        public void Read(Stream dataDestanation)
        {
            Debug.WriteLine("tar stream position Read in: " + inStream.Position);
            int readBytes;
            byte[] read;
            while ((readBytes = Read(out read)) != -1)
            {
                Debug.WriteLine("tar stream position Read while(...) : " + inStream.Position);
                dataDestanation.Write(read, 0, readBytes);
            }
            Debug.WriteLine("tar stream position Read out: " + inStream.Position);
        }

        protected int Read(out byte[] buffer)
        {
            if(remainingBytesInFile == 0)
            {
                buffer = null;
                return -1;
            }
            int align512 = -1;
            long toRead = remainingBytesInFile - 512;

            if (toRead > 0) 
                toRead = 512;
            else
            {
                align512 = 512 - (int)remainingBytesInFile;
                toRead = remainingBytesInFile;
            }
                

            int bytesRead = inStream.Read(dataBuffer, 0, (int)toRead);
            remainingBytesInFile -= bytesRead;
            
            if(inStream.CanSeek && align512 > 0)
            {
                inStream.Seek(align512, SeekOrigin.Current);
            }
            else
                while(align512 > 0)
                {
                    inStream.ReadByte();
                    --align512;
                }
                
            buffer = dataBuffer;
            return bytesRead;
        }

        /// <summary>
        /// Check if all bytes in buffer are zeroes
        /// </summary>
        /// <param name="buffer">buffer to check</param>
        /// <returns>true if all bytes are zeroes, otherwise false</returns>
        private static bool IsEmpty(IEnumerable<byte> buffer)
        {
            foreach(byte b in buffer)
            {
                if (b != 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Move internal pointer to a next file in archive.
        /// </summary>
        /// <param name="skipData">Should be true if you want to read a header only, otherwise false</param>
        /// <returns>false on End Of File otherwise true</returns>
        /// 
        /// Example:
        /// while(MoveNext())
        /// { 
        ///     Read(dataDestStream); 
        /// }
        /// <seealso cref="Read(Stream)"/>
        public bool MoveNext(bool skipData)
        {
            Debug.WriteLine("tar stream position MoveNext in: " + inStream.Position);
            if (remainingBytesInFile > 0)
            {
                if (!skipData)
                {
                    throw new TarException(
                        "You are trying to change file while not all the data from the previous one was read. If you do want to skip files use skipData parameter set to true.");
                }
                // Skip to the end of file.
                if (inStream.CanSeek)
                {
                    long remainer = (remainingBytesInFile%512);
                    inStream.Seek(remainingBytesInFile + (512 - (remainer == 0 ? 512 : remainer) ), SeekOrigin.Current);
                }
                else
                {
                    byte[] buffer;
                    while (Read(out buffer) != -1)
                    {
                    }
                }
            }

            byte[] bytes = header.GetBytes();

            int headerRead = inStream.Read(bytes, 0, header.HeaderSize);
            if (headerRead < 512)
            {
                throw new TarException("Can not read header");
            }

            if(IsEmpty(bytes))
            {
                headerRead = inStream.Read(bytes, 0, header.HeaderSize);
                if(headerRead == 512 && IsEmpty(bytes))
                {
                    Debug.WriteLine("tar stream position MoveNext  out(false): " + inStream.Position);
                    return false;
                }
                throw new TarException("Broken archive");
            }

            if (header.UpdateHeaderFromBytes())
            {
                throw new TarException("Checksum check failed");
            }

            remainingBytesInFile = header.SizeInBytes;

            Debug.WriteLine("tar stream position MoveNext  out(true): " + inStream.Position);
            return true;
        }
    }
}