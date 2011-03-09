/*
 * The MIT License
 *
 * Copyright (c) 2011 Emeric Fermas
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
using System.Diagnostics;
using System.Linq;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public static class ObjectId
    {
        private static readonly string HexDigits = "0123456789abcdef";
        private static readonly byte[] ReverseHexDigits = BuildReverseHexDigits();

        private static byte[] BuildReverseHexDigits()
        {
            var bytes = new byte['f' - '0' + 1];

            for (int i = 0; i < 10; i++)
            {
                bytes[i] = (byte)i;
            }

            for (int i = 10; i < 16; i++)
            {
                bytes[i + 'a' - '0' - 0x0a] = (byte)(i);
            }

            return bytes;
        }

        private static readonly Func<int, byte> ByteConverter = i => ReverseHexDigits[i - '0'];

        public static string ToString(byte[] id)
        {
            if (id == null || id.Length != Constants.GIT_OID_RAWSZ)
            {
                throw new ArgumentException("id");
            }

            // Inspired from http://stackoverflow.com/questions/623104/c-byte-to-hex-string/3974535#3974535

            var c = new char[Constants.GIT_OID_HEXSZ];

            for (int i = 0; i < Constants.GIT_OID_HEXSZ; i++)
            {
                int index0 = i >> 1;
                var b = ((byte)(id[index0] >> 4));
                c[i++] = HexDigits[b];

                b = ((byte)(id[index0] & 0x0F));
                c[i] = HexDigits[b];
            }

            return new string(c);
        }

        public static byte[] ToByteArray(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length != Constants.GIT_OID_HEXSZ)
            {
                throw new ArgumentException("id");
            }

            var bytes = new byte[Constants.GIT_OID_RAWSZ];

            for (int i = 0; i < Constants.GIT_OID_HEXSZ; i++)
            {
                int c1 = ByteConverter(id[i++]) << 4;
                int c2 = ByteConverter(id[i]);

                bytes[i >> 1] = (byte)(c1 + c2);
            }

            return bytes;
        }

        public static bool IsValid(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
            {
                return false;
            }

            if (objectId.Length != Constants.GIT_OID_HEXSZ)
            {
                return false;
            }

            return objectId.All(c => HexDigits.Contains(c.ToString()));
        }
    }
}
