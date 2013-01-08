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
using System.Net;
using System.Text;

namespace tar_cs
{
    /// <summary>
    /// UsTar header implementation.
    /// </summary>
    internal class UsTarHeader : TarHeader
    {
        private const string magic = "ustar";
        private const string version = "  ";
        private string groupName;

        private string namePrefix = string.Empty;
        private string userName;

        public override string UserName
        {
            get { return userName.Replace("\0",string.Empty); }
            set
            {
                if (value.Length > 32)
                {
                    throw new TarException("user name can not be longer than 32 chars");
                }
                userName = value;
            }
        }

        public override string GroupName
        {
            get { return groupName.Replace("\0",string.Empty); }
            set
            {
                if (value.Length > 32)
                {
                    throw new TarException("group name can not be longer than 32 chars");
                }
                groupName = value;
            }
        }

        public override string FileName
        {
            get { return namePrefix.Replace("\0", string.Empty) + base.FileName.Replace("\0", string.Empty); }
            set
            {
                if (value.Length > 100)
                {
                    if (value.Length > 255)
                    {
                        throw new TarException("UsTar fileName can not be longer thatn 255 chars");
                    }
                    int position = value.Length - 100;

                    // Find first path separator in the remaining 100 chars of the file name
                    while (!IsPathSeparator(value[position]))
                    {
                        ++position;
                        if (position == value.Length)
                        {
                            break;
                        }
                    }
                    if (position == value.Length)
                        position = value.Length - 100;
                    namePrefix = value.Substring(0, position);
                    base.FileName = value.Substring(position, value.Length - position);
                }
                else
                {
                    base.FileName = value;
                }
            }
        }

        public override bool UpdateHeaderFromBytes()
        {
            byte[] bytes = GetBytes();
            UserName = Encoding.ASCII.GetString(bytes, 0x109, 32);
            GroupName = Encoding.ASCII.GetString(bytes, 0x129, 32);
            namePrefix = Encoding.ASCII.GetString(bytes, 347, 157);
            return base.UpdateHeaderFromBytes();
        }

        private static bool IsPathSeparator(char ch)
        {
            return (ch == '\\' || ch == '/' || ch == '|'); // All the path separators I ever met.
        }

        public override byte[] GetHeaderValue()
        {
            byte[] header = base.GetHeaderValue();

            Encoding.ASCII.GetBytes(magic).CopyTo(header, 0x101); // Mark header as ustar
            Encoding.ASCII.GetBytes(version).CopyTo(header, 0x106);
            Encoding.ASCII.GetBytes(UserName).CopyTo(header, 0x109);
            Encoding.ASCII.GetBytes(GroupName).CopyTo(header, 0x129);
            Encoding.ASCII.GetBytes(namePrefix).CopyTo(header, 347);

            if (SizeInBytes >= 0x1FFFFFFFF)
            {
                byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(SizeInBytes));
                SetMarker(AlignTo12(bytes)).CopyTo(header, 124);
            }

            RecalculateChecksum(header);
            Encoding.ASCII.GetBytes(HeaderChecksumString).CopyTo(header, 148);
            return header;
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