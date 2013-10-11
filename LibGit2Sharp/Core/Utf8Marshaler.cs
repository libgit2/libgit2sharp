using System;
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
    ///                    MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
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
    /// This marshaler is to be used for sending managed String instances to libgit2.
    /// The marshaler will allocate a buffer in native memory to hold the UTF-8 string
    /// and perform the encoding conversion using that buffer as the target. The pointer
    /// received by libgit2 will be to this buffer. After the function call completes, the
    /// native buffer is freed.
    ///
    /// Use this marshaler for function parameters, for example:
    /// [DllImport(libgit2)]
    /// internal static extern int git_tag_delete(RepositorySafeHandle repo,
    ///     [MarshalAs(UnmanagedType.CustomMarshaler,
    ///                MarshalCookie = UniqueId.UniqueIdentifier,
    ///                MarshalTypeRef = typeof(Utf8Marshaler))] String tagName);
    /// </summary>
    internal class Utf8Marshaler : EncodingMarshaler
    {
        private static readonly Utf8Marshaler staticInstance;
        private static readonly Encoding encoding;

        static Utf8Marshaler()
        {
            encoding = Encoding.UTF8;
            staticInstance = new Utf8Marshaler();
        }

        public Utf8Marshaler() : base(encoding)
        { }

        public static ICustomMarshaler GetInstance(String cookie)
        {
            return staticInstance;
        }

        public static IntPtr FromManaged(String value)
        {
            return FromManaged(encoding, value);
        }

        public static string FromNative(IntPtr pNativeData)
        {
            return FromNative(encoding, pNativeData);
        }

        public static String FromNative(IntPtr pNativeData, int length)
        {
            return FromNative(encoding, pNativeData, length);
        }

        public static String Utf8FromBuffer(byte[] buffer)
        {
            return FromBuffer(encoding, buffer);
        }
    }
}
