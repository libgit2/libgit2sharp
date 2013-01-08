using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    /// <summary>
    ///   This marshaler is to be used for capturing a UTF-8 string owned by libgit2 and
    ///   converting it to a managed String instance. The marshaler will not attempt to
    ///   free the native pointer after conversion, because the memory is owned by libgit2.
    ///
    ///   Use this marshaler for return values, for example:
    ///   [return: MarshalAs(UnmanagedType.CustomMarshaler,
    ///                      MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
    /// </summary>
    internal class Utf8NoCleanupMarshaler : Utf8Marshaler
    {
        private static readonly Utf8NoCleanupMarshaler staticInstance = new Utf8NoCleanupMarshaler();

        public new static ICustomMarshaler GetInstance(String cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public override void CleanUpNativeData(IntPtr pNativeData)
        {
        }

        #endregion
    }

    /// <summary>
    ///   This marshaler is to be used for sending managed String instances to libgit2.
    ///   The marshaler will allocate a buffer in native memory to hold the UTF-8 string
    ///   and perform the encoding conversion using that buffer as the target. The pointer
    ///   received by libgit2 will be to this buffer. After the function call completes, the
    ///   native buffer is freed.
    ///
    ///   Use this marshaler for function parameters, for example:
    ///   [DllImport(libgit2)]
    ///   internal static extern int git_tag_delete(RepositorySafeHandle repo,
    ///       [MarshalAs(UnmanagedType.CustomMarshaler,
    ///                  MarshalTypeRef = typeof(Utf8Marshaler))] String tagName);
    /// </summary>
    internal class Utf8Marshaler : ICustomMarshaler
    {
        private static readonly Utf8Marshaler staticInstance = new Utf8Marshaler();

        public static ICustomMarshaler GetInstance(String cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public void CleanUpManagedData(Object managedObj)
        {
        }

        public virtual void CleanUpNativeData(IntPtr pNativeData)
        {
            if (IntPtr.Zero != pNativeData)
            {
                Marshal.FreeHGlobal(pNativeData);
            }
        }

        public int GetNativeDataSize()
        {
            // Not a value type
            return -1;
        }

        public IntPtr MarshalManagedToNative(Object managedObj)
        {
            if (null == managedObj)
            {
                return IntPtr.Zero;
            }

            String str = managedObj as String;

            if (null == str)
            {
                throw new MarshalDirectiveException("Utf8Marshaler must be used on a string.");
            }

            return Utf8Marshaler.FromManaged(str);
        }

        public Object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return Utf8Marshaler.FromNative(pNativeData);
        }

        #endregion

        public static unsafe IntPtr FromManaged(String value)
        {
            if (null == value)
            {
                return IntPtr.Zero;
            }

            int length = Encoding.UTF8.GetByteCount(value);
            byte* buffer = (byte*)Marshal.AllocHGlobal(length + 1).ToPointer();

            if (length > 0)
            {
                fixed (char* pValue = value)
                {
                    Encoding.UTF8.GetBytes(pValue, value.Length, buffer, length);
                }
            }

            buffer[length] = 0;

            return new IntPtr(buffer);
        }

        public static unsafe String FromNative(IntPtr pNativeData)
        {
            if (IntPtr.Zero == pNativeData)
            {
                return null;
            }

            byte* start = (byte*)pNativeData;
            byte* walk = start;

            // Find the end of the string
            while (*walk != 0)
            {
                walk++;
            }

            if (walk == start)
            {
                return String.Empty;
            }

            return new String((sbyte*)pNativeData.ToPointer(), 0, (int)(walk - start), Encoding.UTF8);
        }

        public static unsafe String FromNative(IntPtr pNativeData, int length)
        {
            if (IntPtr.Zero == pNativeData)
            {
                return null;
            }

            if (0 == length)
            {
                return String.Empty;
            }

            return new String((sbyte*)pNativeData.ToPointer(), 0, length, Encoding.UTF8);
        }

        public static String Utf8FromBuffer(byte[] buffer)
        {
            if (null == buffer)
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

            if (0 == length)
            {
                return String.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, length);
        }
    }
}
