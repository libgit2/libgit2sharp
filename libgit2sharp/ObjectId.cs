using System;
using System.Diagnostics;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public static class ObjectId
    {
        private static readonly char[] HexDigits = "0123456789abcdef".ToCharArray();
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

        public static string ToString(byte[] id)
        {
            Debug.Assert(id != null && id.Length == Constants.GIT_OID_RAWSZ);

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
            Debug.Assert(id != null && id.Length == Constants.GIT_OID_HEXSZ);

            var bytes = new byte[Constants.GIT_OID_RAWSZ];

            for (int i = 0; i < Constants.GIT_OID_HEXSZ; i++)
            {
                int c1 = ByteConverter(id[i++]) << 4;
                int c2 = ByteConverter(id[i]);

                bytes[i >> 1] = (byte)(c1 + c2);
            }

            return bytes;
        }

        private static readonly Func<int, byte> ByteConverter = i => ReverseHexDigits[i - '0'];
    }
}