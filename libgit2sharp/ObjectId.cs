using System;
using System.Diagnostics;
using System.Linq;
using libgit2sharp.Wrapper;

namespace libgit2sharp
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