using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// This marshaler is to be used for capturing a UTF-8 string owned by libgit2 and
    /// converting it to a managed FilePath instance. The marshaler will not attempt to
    /// free the native pointer after conversion, because the memory is owned by libgit2.
    ///
    /// Use this marshaler for return values, for example:
    /// [return: MarshalAs(UnmanagedType.CustomMarshaler,
    ///                    MarshalCookie = UniqueId.UniqueIdentifier,
    ///                    MarshalTypeRef = typeof(LaxFilePathNoCleanupMarshaler))]
    /// </summary>
    internal class LaxFilePathNoCleanupMarshaler : LaxFilePathMarshaler
    {
        private static readonly LaxFilePathNoCleanupMarshaler staticInstance = new LaxFilePathNoCleanupMarshaler();

        public new static ICustomMarshaler GetInstance(string cookie)
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
    /// This marshaler is to be used for sending managed FilePath instances to libgit2.
    /// The marshaler will allocate a buffer in native memory to hold the UTF-8 string
    /// and perform the encoding conversion using that buffer as the target. The pointer
    /// received by libgit2 will be to this buffer. After the function call completes, the
    /// native buffer is freed.
    ///
    /// Use this marshaler for function parameters, for example:
    /// [DllImport(libgit2)]
    /// internal static extern int git_index_open(out IndexSafeHandle index,
    ///     [MarshalAs(UnmanagedType.CustomMarshaler,
    ///                MarshalCookie = UniqueId.UniqueIdentifier,
    ///                MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath indexpath);
    /// </summary>
    internal class StrictFilePathMarshaler : StrictUtf8Marshaler
    {
        private static readonly StrictFilePathMarshaler staticInstance = new StrictFilePathMarshaler();

        public new static ICustomMarshaler GetInstance(string cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public override IntPtr MarshalManagedToNative(object managedObj)
        {
            if (null == managedObj)
            {
                return IntPtr.Zero;
            }

            var filePath = managedObj as FilePath;

            if (null == filePath)
            {
                throw new MarshalDirectiveException(string.Format(CultureInfo.InvariantCulture,
                                                    "{0} must be used on a FilePath.",
                                                    this.GetType().Name));
            }

            return FromManaged(filePath);
        }

        #endregion

        public static IntPtr FromManaged(FilePath filePath)
        {
            if (filePath == null)
            {
                return IntPtr.Zero;
            }

            return StrictUtf8Marshaler.FromManaged(filePath.Posix);
        }
    }

    /// <summary>
    /// This marshaler is to be used for capturing a UTF-8 string allocated by libgit2 and
    /// converting it to a managed FilePath instance. The marshaler will free the native pointer
    /// after conversion.
    /// </summary>
    internal class LaxFilePathMarshaler : LaxUtf8Marshaler
    {
        private static readonly LaxFilePathMarshaler staticInstance = new LaxFilePathMarshaler();

        public new static ICustomMarshaler GetInstance(string cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public override object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return FromNative(pNativeData);
        }

        #endregion

        public new static FilePath FromNative(IntPtr pNativeData)
        {
            return LaxUtf8Marshaler.FromNative(pNativeData);
        }

        public new static unsafe FilePath FromNative(char* buffer)
        {
            return LaxUtf8Marshaler.FromNative(buffer);
        }

        public new static FilePath FromBuffer(byte[] buffer)
        {
            return LaxUtf8Marshaler.FromBuffer(buffer);
        }
    }
}
