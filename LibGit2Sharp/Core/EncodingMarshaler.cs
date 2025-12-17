using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    internal abstract class EncodingMarshaler : ICustomMarshaler
    {
        private readonly Encoding encoding;

        protected EncodingMarshaler(Encoding encoding)
        {
            this.encoding = encoding;
        }

        #region ICustomMarshaler

        public void CleanUpManagedData(object managedObj)
        {
        }

        public virtual void CleanUpNativeData(IntPtr pNativeData)
        {
            Cleanup(pNativeData);
        }

        public int GetNativeDataSize()
        {
            // Not a value type
            return -1;
        }

        public virtual IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
            {
                return IntPtr.Zero;
            }

            var str = managedObj as string;

            if (str == null)
            {
                throw new MarshalDirectiveException(string.Format(CultureInfo.InvariantCulture,
                                                                  "{0} must be used on a string.",
                                                                  GetType().Name));
            }

            return FromManaged(encoding, str);
        }

        public virtual object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return FromNative(encoding, pNativeData);
        }

        #endregion

        public static unsafe IntPtr FromManaged(Encoding encoding, string value)
        {
            if (encoding == null || value == null)
            {
                return IntPtr.Zero;
            }

            int length = encoding.GetByteCount(value);
            var buffer = (byte*)Marshal.AllocHGlobal(length + 1).ToPointer();

            if (length > 0)
            {
                fixed (char* pValue = value)
                {
                    encoding.GetBytes(pValue, value.Length, buffer, length);
                }
            }

            buffer[length] = 0;

            return new IntPtr(buffer);
        }

        public static void Cleanup(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return;
            }

            Marshal.FreeHGlobal(pNativeData);
        }

        public static unsafe string FromNative(Encoding encoding, IntPtr pNativeData)
        {
            return FromNative(encoding, (byte*)pNativeData);
        }

        public static unsafe string FromNative(Encoding encoding, byte* pNativeData)
        {
            if (pNativeData == null)
            {
                return null;
            }

            var start = (byte*)pNativeData;
            byte* walk = start;

            // Find the end of the string
            while (*walk != 0)
            {
                walk++;
            }

            if (walk == start)
            {
                return string.Empty;
            }

            return new string((sbyte*)pNativeData, 0, (int)(walk - start), encoding);
        }

        public static unsafe string FromNative(Encoding encoding, IntPtr pNativeData, int length)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return null;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            return new string((sbyte*)pNativeData.ToPointer(), 0, length, encoding);
        }

        public static string FromBuffer(Encoding encoding, byte[] buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            int length = 0;
            int stop = buffer.Length;

            while (length < stop &&
                   0 != buffer[length])
            {
                length++;
            }

            return FromBuffer(encoding, buffer, length);
        }

        public static string FromBuffer(Encoding encoding, byte[] buffer, int length)
        {
            Debug.Assert(buffer != null);

            if (length == 0)
            {
                return string.Empty;
            }

            return encoding.GetString(buffer, 0, length);
        }
    }
}
