using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    ///   This marshaler is to be used for capturing a UTF-8 string owned by libgit2 and
    ///   converting it to a managed FilePath instance. The marshaler will not attempt to
    ///   free the native pointer after conversion, because the memory is owned by libgit2.
    ///
    ///   Use this marshaler for return values, for example:
    ///   [return: MarshalAs(UnmanagedType.CustomMarshaler,
    ///                      MarshalTypeRef = typeof(FilePathNoCleanupMarshaler))]
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
    ///   This marshaler is to be used for sending managed FilePath instances to libgit2.
    ///   The marshaler will allocate a buffer in native memory to hold the UTF-8 string
    ///   and perform the encoding conversion using that buffer as the target. The pointer
    ///   received by libgit2 will be to this buffer. After the function call completes, the
    ///   native buffer is freed.
    ///
    ///   Use this marshaler for function parameters, for example:
    ///   [DllImport(libgit2)]
    ///   internal static extern int git_index_open(out IndexSafeHandle index,
    ///       [MarshalAs(UnmanagedType.CustomMarshaler,
    ///                  MarshalTypeRef = typeof(FilePathMarshaler))] FilePath indexpath);
    /// </summary>
    internal class FilePathMarshaler : ICustomMarshaler
    {
        private static readonly FilePathMarshaler staticInstance = new FilePathMarshaler();

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

            FilePath filePath = managedObj as FilePath;

            if (null == filePath)
            {
                throw new MarshalDirectiveException("FilePathMarshaler must be used on a FilePath.");
            }

            return FilePathMarshaler.FromManaged(filePath);
        }

        public Object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return FilePathMarshaler.FromNative(pNativeData);
        }

        #endregion

        public static unsafe IntPtr FromManaged(FilePath filePath)
        {
            if (null == filePath)
            {
                return IntPtr.Zero;
            }

            return Utf8Marshaler.FromManaged(filePath.Posix);
        }

        public static unsafe FilePath FromNative(IntPtr pNativeData)
        {
            if (IntPtr.Zero == pNativeData)
            {
                return null;
            }

            if (0 == Marshal.ReadByte(pNativeData))
            {
                return FilePath.Empty;
            }

            return (FilePath)Utf8Marshaler.FromNative(pNativeData);
        }

        public static unsafe FilePath FromNative(IntPtr pNativeData, int length)
        {
            if (0 == length)
            {
                return FilePath.Empty;
            }

            return (FilePath)Utf8Marshaler.FromNative(pNativeData, length);
        }
    }
}
