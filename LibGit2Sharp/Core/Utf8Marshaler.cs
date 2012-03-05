using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    internal class Utf8Marshaler : ICustomMarshaler
    {
        static Utf8Marshaler staticInstance;
        readonly bool ownsPointer;

        internal Utf8Marshaler(bool ownsPointer = false)
        {
            this.ownsPointer = ownsPointer;
        }

        #region ICustomMarshaler Members

        public unsafe IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
            {
                return IntPtr.Zero;
            }

            if (!(managedObj is string))
            {
                throw new MarshalDirectiveException("UTF8Marshaler must be used on a string.");
            }

            // not null terminated
            byte[] strbuf = Encoding.UTF8.GetBytes((string)managedObj);
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length + 1);
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);

            // write the terminating null
            var pBuf = (byte*)buffer;
            pBuf[strbuf.Length] = 0;

            return buffer;
        }

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            var walk = (byte*)pNativeData;

            // find the end of the string
            while (*walk != 0)
            {
                walk++;
            }
            var length = (int)(walk - (byte*)pNativeData);

            // should not be null terminated
            var strbuf = new byte[length];

            // skip the trailing null
            Marshal.Copy(pNativeData, strbuf, 0, length);
            string data = Encoding.UTF8.GetString(strbuf);
            return data;
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (ownsPointer)
                Marshal.FreeHGlobal(pNativeData);
        }

        public void CleanUpManagedData(object managedObj)
        {
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        #endregion

        public static ICustomMarshaler GetInstance(string cookie)
        {
            if (staticInstance == null)
            {
                return staticInstance = new Utf8Marshaler();
            }

            return staticInstance;
        }

        public static string Utf8FromBuffer(byte[] buffer)
        {
            int nullTerminator;
            for (nullTerminator = 0; nullTerminator < buffer.Length; nullTerminator++)
            {
                if (buffer[nullTerminator] == 0)
                {
                    break;
                }
            }

            if (nullTerminator == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(buffer, 0, nullTerminator);
        }
    }
}
