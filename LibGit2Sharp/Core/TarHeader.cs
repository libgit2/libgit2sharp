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
using System.Diagnostics;
using System.Net;
using System.Text;
using tar_cs;

namespace tar_cs
{
    internal class TarHeader : ITarHeader
    {
        private readonly byte[] buffer = new byte[512];
        private long headerChecksum;

        public TarHeader()
        {
            // Default values
            Mode = 511; // 0777 dec
            UserId = 61; // 101 dec
            GroupId = 61; // 101 dec
        }

        private string fileName;
        protected readonly DateTime TheEpoch = new DateTime(1970, 1, 1, 0, 0, 0);

        public virtual string FileName
        {
            get
            {
                return fileName.Replace("\0",string.Empty);
            } 
            set
            {
                if(value.Length > 100)
                {
                    throw new TarException("A file name can not be more than 100 chars long");
                }
                fileName = value;
            }
        }
        public int Mode { get; set; }

        public string ModeString
        {
            get { return AddChars(Convert.ToString(Mode, 8), 7, '0', true); }
        }

        public int UserId { get; set; }
        public virtual string UserName
        {
            get { return UserId.ToString(); }
            set { UserId = Int32.Parse(value); }
        }

        public string UserIdString
        {
            get { return AddChars(Convert.ToString(UserId, 8), 7, '0', true); }
        }

        public int GroupId { get; set; }
        public virtual string GroupName
        {
            get { return GroupId.ToString(); }
            set { GroupId = Int32.Parse(value); }
        }

        public string GroupIdString
        {
            get { return AddChars(Convert.ToString(GroupId, 8), 7, '0', true); }
        }

        public long SizeInBytes { get; set; }

        public string SizeString
        {
            get { return AddChars(Convert.ToString(SizeInBytes, 8), 11, '0', true); }
        }

        public DateTime LastModification { get; set; }

        public string LastModificationString
        {
            get
            {
                return AddChars(
                    ((long) (LastModification - TheEpoch).TotalSeconds).ToString(), 11, '0',
                    true);
            }
        }

        public string HeaderChecksumString
        {
            get { return AddChars(Convert.ToString(headerChecksum, 8), 6, '0', true); }
        }


        public virtual int HeaderSize
        {
            get { return 512; }
        }

        private static string AddChars(string str, int num, char ch, bool isLeading)
        {
            int neededZeroes = num - str.Length;
            while (neededZeroes > 0)
            {
                if (isLeading)
                    str = ch + str;
                else
                    str = str + ch;
                --neededZeroes;
            }
            return str;
        }

        public byte[] GetBytes()
        {
            return buffer;
        }

        public virtual bool UpdateHeaderFromBytes()
        {
            FileName = Encoding.ASCII.GetString(buffer, 0, 100);
            Mode = Convert.ToInt32(Encoding.ASCII.GetString(buffer, 100, 7), 8);
            UserId = Convert.ToInt32(Encoding.ASCII.GetString(buffer, 108, 7), 8);
            GroupId = Convert.ToInt32(Encoding.ASCII.GetString(buffer, 116, 7), 8);
            if((buffer[124] & 0x80) == 0x80) // if size in binary
            {
                long sizeBigEndian = BitConverter.ToInt64(buffer,0x80);
                SizeInBytes = IPAddress.NetworkToHostOrder(sizeBigEndian);
            }
            else
            {
                SizeInBytes = Convert.ToInt64(Encoding.ASCII.GetString(buffer, 124, 11), 8);
            }
            long unixTimeStamp = Convert.ToInt64(Encoding.ASCII.GetString(buffer,136,11));
            LastModification = TheEpoch.AddSeconds(unixTimeStamp);

            var storedChecksum = Convert.ToInt32(Encoding.ASCII.GetString(buffer,148,6));
            RecalculateChecksum(buffer);
            if (storedChecksum == headerChecksum)
            {
                return true;
            }

            RecalculateAltChecksum(buffer);
            return storedChecksum == headerChecksum;
        }

        private void RecalculateAltChecksum(byte[] buf)
        {
            Encoding.ASCII.GetBytes("        ").CopyTo(buf, 148);
            headerChecksum = 0;
            foreach(byte b in buf)
            {
                if((b & 0x80) == 0x80)
                {
                    headerChecksum -= b ^ 0x80;
                }
                else
                {
                    headerChecksum += b;
                }
            }
        }

        public virtual byte[] GetHeaderValue()
        {
            // Clean old values
            int i = 0;
            while (i < 512)
            {
                buffer[i] = 0;
                ++i;
            }

            if (string.IsNullOrEmpty(FileName)) throw new TarException("FileName can not be empty.");
            if (FileName.Length >= 100) throw new TarException("FileName is too long. It must be less than 100 bytes.");

            // Fill header
            Encoding.ASCII.GetBytes(AddChars(FileName, 100, '\0', false)).CopyTo(buffer, 0);
            Encoding.ASCII.GetBytes(ModeString).CopyTo(buffer, 100);
            Encoding.ASCII.GetBytes(UserIdString).CopyTo(buffer, 108);
            Encoding.ASCII.GetBytes(GroupIdString).CopyTo(buffer, 116);
            Encoding.ASCII.GetBytes(SizeString).CopyTo(buffer, 124);
            Encoding.ASCII.GetBytes(LastModificationString).CopyTo(buffer, 136);

            RecalculateChecksum(buffer);

            // Write checksum
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(buffer, 148);

            return buffer;
        }

        protected virtual void RecalculateChecksum(byte[] buf)
        {
// Set default value for checksum. That is 8 spaces.
            Encoding.ASCII.GetBytes("        ").CopyTo(buf, 148);

            // Calculate checksum
            headerChecksum = 0;
            foreach (byte b in buf)
            {
                headerChecksum += b;
            }
        }
    }
}