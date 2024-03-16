using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// This marshaler is to be used for capturing a UTF-8 string owned by libgit2 and
    /// converting it to a managed String instance. The marshaler will not attempt to
    /// free the native pointer after conversion, because the memory is owned by libgit2.
    ///
    /// Use this marshaler for return values, for example:
    /// [return: MarshalAs(UnmanagedType.CustomMarshaler,
    ///                    MarshalCookie = UniqueId.UniqueIdentifier,
    ///                    MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
    /// </summary>
    internal class LaxUtf8NoCleanupMarshaler : LaxUtf8Marshaler
    {
        private static readonly LaxUtf8NoCleanupMarshaler staticInstance = new LaxUtf8NoCleanupMarshaler();

        public new static ICustomMarshaler GetInstance(string cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public override void CleanUpNativeData(IntPtr pNativeData)
        { }

        #endregion
    }

    /// <summary>
    /// This marshaler is to be used for sending managed String instances to libgit2.
    /// The marshaler will allocate a buffer in native memory to hold the UTF-8 string
    /// and perform the encoding conversion using that buffer as the target. The pointer
    /// received by libgit2 will be to this buffer. After the function call completes, the
    /// native buffer is freed.
    ///
    /// Use this marshaler for function parameters, for example:
    /// [DllImport(libgit2)]
    /// internal static extern int git_tag_delete(RepositorySafeHandle repo,
    ///     [MarshalAs(UnmanagedType.CustomMarshaler
    ///                MarshalCookie = UniqueId.UniqueIdentifier,
    ///                MarshalTypeRef = typeof(StrictUtf8Marshaler))] String tagName);
    /// </summary>
    internal class StrictUtf8Marshaler : EncodingMarshaler
    {
        private static readonly StrictUtf8Marshaler staticInstance;
        private static readonly Encoding encoding;

        static StrictUtf8Marshaler()
        {
            encoding = new UTF8Encoding(false, true);
            staticInstance = new StrictUtf8Marshaler();
        }

        public StrictUtf8Marshaler() : base(encoding)
        { }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public override object MarshalNativeToManaged(IntPtr pNativeData)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                              "{0} cannot be used to retrieve data from libgit2.",
                                                              GetType().Name));
        }

        #endregion

        public static IntPtr FromManaged(string value)
        {
            return FromManaged(encoding, value);
        }
    }

    /// <summary>
    /// This marshaler is to be used for capturing a UTF-8 string allocated by libgit2 and
    /// converting it to a managed String instance. The marshaler will free the native pointer
    /// after conversion.
    /// </summary>
    internal class LaxUtf8Marshaler : EncodingMarshaler
    {
        private static readonly LaxUtf8Marshaler staticInstance = new LaxUtf8Marshaler();

        public static readonly Encoding Encoding = new UTF8Encoding(false, false);

        public LaxUtf8Marshaler() : base(Encoding)
        { }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public override IntPtr MarshalManagedToNative(object managedObj)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                              "{0} cannot be used to pass data to libgit2.",
                                                              GetType().Name));
        }

        #endregion

        public static unsafe string FromNative(char* pNativeData)
        {
            return FromNative(Encoding, (byte*)pNativeData);
        }

        public static string FromNative(IntPtr pNativeData)
        {
            return FromNative(Encoding, pNativeData);
        }

        public static string FromNative(IntPtr pNativeData, int length)
        {
            return FromNative(Encoding, pNativeData, length);
        }

        public static string FromBuffer(byte[] buffer)
        {
            return FromBuffer(Encoding, buffer);
        }

        public static string FromBuffer(byte[] buffer, int length)
        {
            return FromBuffer(Encoding, buffer, length);
        }
    }
}
