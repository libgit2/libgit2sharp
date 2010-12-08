using System.Diagnostics;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public static class ObjectId
    {
        private static readonly char[] hexDigits = new []
                                                       {
                                                           '0', '1', '2', '3', '4', '5', '6', '7', '8', 
                                                           '9', 'a', 'b', 'c', 'd', 'e', 'f'
                                                       };

        public static string ToString(byte[] id)
        {
            Debug.Assert(id != null && id.Length == Constants.GIT_OID_RAWSZ);

            // Inspired from http://stackoverflow.com/questions/623104/c-byte-to-hex-string/3974535#3974535

            var c = new char[Constants.GIT_OID_RAWSZ * 2];

            for (int i = 0; i < Constants.GIT_OID_RAWSZ * 2; i++)
            {
                int index0 = i >> 1;
                var b = ((byte)(id[index0] >> 4));
                c[i++] = hexDigits[b];

                b = ((byte)(id[index0] & 0x0F));
                c[i] = hexDigits[b];
            }

            return new string(c);
        }
    }
}