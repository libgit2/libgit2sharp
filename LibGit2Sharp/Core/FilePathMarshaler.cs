using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal class FilePathMarshaler : Utf8Marshaler
    {
        private static FilePathMarshaler staticInstance;

        public override IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
            {
                return IntPtr.Zero;
            }

            if (!(managedObj is FilePath))
            {
                throw new MarshalDirectiveException("FilePathMarshaler must be used on a FilePath.");
            }

            return StringToNative(((FilePath)managedObj).Posix);
        }

        public override object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return (FilePath)NativeToString(pNativeData);
        }

        public static new ICustomMarshaler GetInstance(string cookie)
        {
            if (staticInstance == null)
            {
                return staticInstance = new FilePathMarshaler();
            }

            return staticInstance;
        }
    }
}