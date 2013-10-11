using System;
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
    ///                    MarshalTypeRef = typeof(FilePathNoCleanupMarshaler))]
    /// </summary>
    internal class FilePathNoCleanupMarshaler : FilePathMarshaler
    {
        private static readonly FilePathNoCleanupMarshaler staticInstance = new FilePathNoCleanupMarshaler();

        public static new ICustomMarshaler GetInstance(String cookie)
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
    ///                MarshalTypeRef = typeof(FilePathMarshaler))] FilePath indexpath);
    /// </summary>
    internal class FilePathMarshaler : Utf8Marshaler
    {
        private static readonly FilePathMarshaler staticInstance = new FilePathMarshaler();

        public new static ICustomMarshaler GetInstance(String cookie)
        {
            return staticInstance;
        }

        #region ICustomMarshaler

        public override IntPtr MarshalManagedToNative(Object managedObj)
        {
            if (null == managedObj)
            {
                return IntPtr.Zero;
            }

            var filePath = managedObj as FilePath;

            if (null == filePath)
            {
                throw new MarshalDirectiveException(
                    string.Format("{0} must be used on a FilePath.", GetType().Name));
            }

            return FromManaged(filePath);
        }

        public override Object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return (FilePath)FromNative(pNativeData);
        }

        #endregion

        public static IntPtr FromManaged(FilePath filePath)
        {
            if (filePath == null)
            {
                return IntPtr.Zero;
            }

            return Utf8Marshaler.FromManaged(filePath.Posix);
        }
    }
}
